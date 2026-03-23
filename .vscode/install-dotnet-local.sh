#!/bin/bash
set -e

INSTALL_DIR="$HOME/.dotnet-local"
DOTNET_CMD="$INSTALL_DIR/dotnet"

# Installa sempre l'SDK 10 (idempotente)
echo ">>> Installazione .NET 10 SDK in $INSTALL_DIR (nessun sudo richiesto)..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$INSTALL_DIR"

# Controlla se il workload maui è già installato
if DOTNET_ROOT="$INSTALL_DIR" "$DOTNET_CMD" workload list 2>/dev/null | grep -q "^maui"; then
  echo ">>> Workload MAUI già installato."
else
  echo ">>> Installazione workload MAUI..."
  DOTNET_ROOT="$INSTALL_DIR" "$DOTNET_CMD" workload install maui
fi

echo ">>> Setup completato. DOTNET_ROOT=$INSTALL_DIR"
