#!/bin/bash

# Load environment variables from .env file in the same directory
source "$(dirname "$0")/.env"

if /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1" > /dev/null 2>&1; then
  exit 0
else
  exit 1
fi
