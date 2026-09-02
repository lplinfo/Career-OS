#!/usr/bin/env bash
set -euo pipefail

BAO_ADDR="${BAO_ADDR:-${VAULT_ADDR:-http://127.0.0.1:8200}}"
BAO_TOKEN="${BAO_TOKEN:-${VAULT_TOKEN:-root}}"

echo "==> OpenBao Bootstrap starting on ${BAO_ADDR}..."

# Wait for OpenBao to be healthy
MAX_RETRIES=15
COUNTER=0
until curl -sSf "${BAO_ADDR}/v1/sys/health" > /dev/null 2>&1; do
    COUNTER=$((COUNTER + 1))
    if [ ${COUNTER} -ge ${MAX_RETRIES} ]; then
        echo "Error: OpenBao at ${BAO_ADDR} is not healthy after ${MAX_RETRIES} attempts."
        exit 1
    fi
    echo "Waiting for OpenBao to be ready (${COUNTER}/${MAX_RETRIES})..."
    sleep 1
done

# 1. Enable KV v2 at secret/ if not already mounted
MOUNTS_JSON=$(curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" "${BAO_ADDR}/v1/sys/mounts")
if ! echo "${MOUNTS_JSON}" | grep -q '"secret/"'; then
    echo "==> Mounting KV v2 at secret/..."
    curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
         -H "Content-Type: application/json" \
         -X POST \
         -d '{"type": "kv", "options": {"version": "2"}}' \
         "${BAO_ADDR}/v1/sys/mounts/secret" > /dev/null
else
    echo "==> KV v2 mount 'secret/' already exists."
fi

# 2. Seed secrets (careeros/database, careeros/jwt, careeros/auth-google)
DB_CONN_STR="${POSTGRES_CONNECTION_STRING:-Host=localhost;Port=5432;Database=careeros;Username=career_user;Password=career_password_dev_2026!}"
JWT_SECRET_KEY="${JWT_SECRET_KEY:-$(openssl rand -hex 32)}"
GOOGLE_CLIENT_ID="${GOOGLE_CLIENT_ID:-provisional-google-client-id}"
GOOGLE_CLIENT_SECRET="${GOOGLE_CLIENT_SECRET:-provisional-google-client-secret}"

echo "==> Seeding secret/data/careeros/database..."
curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
     -H "Content-Type: application/json" \
     -X POST \
     -d "{\"data\": {\"connectionstring\": \"${DB_CONN_STR}\"}}" \
     "${BAO_ADDR}/v1/secret/data/careeros/database" > /dev/null

echo "==> Seeding secret/data/careeros/jwt..."
curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
     -H "Content-Type: application/json" \
     -X POST \
     -d "{\"data\": {\"secretkey\": \"${JWT_SECRET_KEY}\"}}" \
     "${BAO_ADDR}/v1/secret/data/careeros/jwt" > /dev/null

echo "==> Seeding secret/data/careeros/auth-google..."
curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
     -H "Content-Type: application/json" \
     -X POST \
     -d "{\"data\": {\"clientid\": \"${GOOGLE_CLIENT_ID}\", \"clientsecret\": \"${GOOGLE_CLIENT_SECRET}\"}}" \
     "${BAO_ADDR}/v1/secret/data/careeros/auth-google" > /dev/null

# 3. Create or update policy careeros-read
echo "==> Creating policy careeros-read..."
curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
     -H "Content-Type: application/json" \
     -X PUT \
     -d '{"policy": "path \"secret/data/careeros/*\" { capabilities = [\"read\"] }\npath \"secret/data/careeros\" { capabilities = [\"read\"] }"}' \
     "${BAO_ADDR}/v1/sys/policies/acl/careeros-read" > /dev/null

# 4. Enable AppRole auth method if not already enabled
AUTH_JSON=$(curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" "${BAO_ADDR}/v1/sys/auth")
if ! echo "${AUTH_JSON}" | grep -q '"approle/"'; then
    echo "==> Enabling AppRole auth method..."
    curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
         -H "Content-Type: application/json" \
         -X POST \
         -d '{"type": "approle"}' \
         "${BAO_ADDR}/v1/sys/auth/approle" > /dev/null
else
    echo "==> AppRole auth method already enabled."
fi

# 5. Create or update role careeros
echo "==> Configuring AppRole role 'careeros'..."
curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" \
     -H "Content-Type: application/json" \
     -X POST \
     -d '{"secret_id_ttl": "0", "token_policies": ["careeros-read"]}' \
     "${BAO_ADDR}/v1/auth/approle/role/careeros" > /dev/null

# 6. Retrieve role_id and generate secret_id
ROLE_ID_RESP=$(curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" "${BAO_ADDR}/v1/auth/approle/role/careeros/role-id")
if ! echo "${ROLE_ID_RESP}" | grep -q '"role_id"'; then
    echo "Error: Response from /v1/auth/approle/role/careeros/role-id does not contain 'role_id'."
    echo "Response: ${ROLE_ID_RESP}"
    exit 1
fi

ROLE_ID=$(echo "${ROLE_ID_RESP}" | grep -o '"role_id":"[^"]*' | cut -d'"' -f4 || true)
if [ -z "${ROLE_ID}" ]; then
    echo "Error: Failed to extract ROLE_ID from response."
    exit 1
fi

SECRET_ID_RESP=$(curl -sSf -H "X-Vault-Token: ${BAO_TOKEN}" -X POST "${BAO_ADDR}/v1/auth/approle/role/careeros/secret-id")
if ! echo "${SECRET_ID_RESP}" | grep -q '"secret_id"'; then
    echo "Error: Response from /v1/auth/approle/role/careeros/secret-id does not contain 'secret_id'."
    echo "Response: ${SECRET_ID_RESP}"
    exit 1
fi

SECRET_ID=$(echo "${SECRET_ID_RESP}" | grep -o '"secret_id":"[^"]*' | cut -d'"' -f4 || true)
if [ -z "${SECRET_ID}" ]; then
    echo "Error: Failed to extract SECRET_ID from response."
    exit 1
fi

echo ""
echo "========================================="
echo "OpenBao Bootstrap Completed Successfully"
echo "========================================="
echo "ROLE_ID=${ROLE_ID}"
echo "SECRET_ID=${SECRET_ID}"
echo "========================================="
