#!/usr/bin/env bash
# 💾 نسخة احتياطية يومية لقاعدة البيانات
set -euo pipefail

cd ~/app
source .env                                 # بياخد DB_NAME و DB_PASSWORD

FILE="db-$(date +%F).bak"                   # مثال: db-2026-11-07.bak
DIR="/var/opt/mssql/backup"

# نعمل الفولدر لو مش موجود
docker compose exec -T sqlserver mkdir -p "$DIR"

# ناخد النسخة
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$DIR/$FILE' WITH INIT"

# نحتفظ بآخر 7 نسخ بس، والأقدم نمسحه
docker compose exec -T sqlserver bash -c "ls -1t $DIR/*.bak | tail -n +8 | xargs -r rm --"

echo "✅ Backup done: $FILE"