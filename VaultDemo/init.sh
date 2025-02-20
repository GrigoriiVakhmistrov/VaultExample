#!/bin/bash

# Проверка наличия Docker и Docker Compose
if ! command -v docker &> /dev/null; then
    echo "Ошибка: Docker не установлен!"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "Ошибка: Docker Compose не установлен!"
    exit 1
fi

# Запуск Vault
# docker-compose up -d

# Ожидание доступности Vault
echo "Ожидание запуска Vault..."
while ! curl --silent --fail http://127.0.0.1:8200/v1/sys/health &> /dev/null; do
    sleep 1
done

# Инициализация Vault
echo "Инициализация Vault..."
INIT_RESPONSE=$(docker exec vault vault operator init -key-shares=1 -key-threshold=1 -format=json)

UNSEAL_KEY=prPjhw67TCph8rsnkfN9+PS7nHwLTSNGpVyBwwBHMDk= #$(echo $INIT_RESPONSE | jq -r '.unseal_keys_b64[0]')
ROOT_TOKEN=root #$(echo $INIT_RESPONSE | jq -r '.root_token')

# Распечатывание Vault
echo "Распечатывание Vault..."
docker exec vault vault operator unseal $UNSEAL_KEY

# Экспорт root token
export VAULT_ADDR='http://127.0.0.1:8200'
export VAULT_TOKEN=$ROOT_TOKEN

# Включение KV v2 с ограничением на 2 версии
echo "Настройка KV v2 хранилища..."
docker exec vault vault secrets enable -path=secret kv-v2

# Добавление секретов
echo "Добавление секретов..."
docker exec vault vault kv put secret/my-secret key1=value1
docker exec vault vault secret/my-secret tune -max-versions=2 secret
docker exec vault vault kv put secret/my-secret key1=value2

echo "Готово!"
echo "Unseal Key: $UNSEAL_KEY"
echo "Root Token: $ROOT_TOKEN"
echo "Адрес Vault: http://localhost:8200"