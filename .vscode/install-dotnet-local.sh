#!/bin/bash
set -e

INSTALL_DIR="$HOME/.dotnet-local"
DOTNET_CMD="$INSTALL_DIR/dotnet"

if [ ! -f "$DOTNET_CMD" ]; then
  echo ">>> Installazione .NET 8 SDK in $INSTALL_DIR (nessun sudo richiesto)..."
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$INSTALL_DIR"
else
  echo ">>> .NET 8 SDK già presente in $INSTALL_DIR"
fi

# Controlla se il workload maui è già installato
if DOTNET_ROOT="$INSTALL_DIR" "$DOTNET_CMD" workload list 2>/dev/null | grep -q "^maui"; then
  echo ">>> Workload MAUI già installato."
else
  echo ">>> Installazione workload MAUI..."
  DOTNET_ROOT="$INSTALL_DIR" "$DOTNET_CMD" workload install maui
fi

echo ">>> Setup completato. DOTNET_ROOT=$INSTALL_DIR"
