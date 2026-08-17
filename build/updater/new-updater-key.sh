#!/usr/bin/env bash
# Génère la paire de clés RSA de l'updater TraceZero.
#
#   - updater.private.pem  : clé PRIVÉE — signe les manifestes. NE JAMAIS committer
#                            (déjà couverte par .gitignore : *.private.pem).
#   - updater.public.pem   : clé PUBLIQUE — à coller dans UpdaterConfig.PublicKeyPem.
#
# La signature est RSA-SHA256 / PKCS#1 v1.5, compatible avec UpdateChecker (VerifyData Pkcs1 SHA256).
#
# Usage :  bash build/updater/new-updater-key.sh [dossier_sortie]
set -euo pipefail

OUT_DIR="${1:-$(pwd)}"
PRIV="$OUT_DIR/updater.private.pem"
PUB="$OUT_DIR/updater.public.pem"

if [[ -f "$PRIV" ]]; then
  echo "Refus : $PRIV existe déjà (ne pas écraser une clé de production)." >&2
  exit 1
fi

openssl genrsa -out "$PRIV" 3072
openssl rsa -in "$PRIV" -pubout -out "$PUB"
chmod 600 "$PRIV" 2>/dev/null || true

echo ""
echo "Clé privée  : $PRIV   (SECRÈTE — sauvegarde hors dépôt, ne jamais committer)"
echo "Clé publique: $PUB"
echo ""
echo "Étape suivante : coller le contenu de la clé publique dans"
echo "  src/TraceZero.App/Services/UpdaterConfig.cs  →  PublicKeyPem"
