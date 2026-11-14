#!/bin/sh
set -eu

port="${PORT:-10000}"
case "$port" in
    ''|*[!0-9]*) echo "PORT debe ser un numero entero." >&2; exit 1 ;;
esac
if [ "${#port}" -gt 5 ] || [ "$port" -lt 1024 ] || [ "$port" -gt 65535 ]; then
    echo "PORT debe estar entre 1024 y 65535 para el usuario sin privilegios." >&2
    exit 1
fi

exec dotnet BodeGo.API.dll --urls "http://0.0.0.0:$port" "$@"
