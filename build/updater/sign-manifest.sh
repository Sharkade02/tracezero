#!/usr/bin/env bash
# Produit un manifeste de mise à jour SIGNÉ pour TraceZero (manifest.json).
#
# Le contenu signé reproduit EXACTEMENT UpdateChecker.SignedPayload :
#     Version \n Channel \n Url \n Sha256 \n MinimumSupportedVersion(ou vide)
# (séparateurs LF, UTF-8, sans saut de ligne final), signé en RSA-SHA256/PKCS#1 v1.5, base64.
#
# Usage :
#   bash build/updater/sign-manifest.sh \
#      --key updater.private.pem \
#      --version 0.1.0 \
#      --channel stable \
#      --url https://github.com/OWNER/REPO/releases/download/v0.1.0/TraceZero-portable.zip \
#      --sha256 <SHA256_DU_ZIP> \
#      [--min 0.1.0] \
#      [--out manifest.json]
#
# Le SHA-256 doit être celui du binaire téléchargé (voir artifacts/SHA256SUMS.txt produit par release.ps1).
set -euo pipefail

KEY="" VERSION="" CHANNEL="stable" URL="" SHA256="" MIN="" OUT="manifest.json"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --key) KEY="$2"; shift 2;;
    --version) VERSION="$2"; shift 2;;
    --channel) CHANNEL="$2"; shift 2;;
    --url) URL="$2"; shift 2;;
    --sha256) SHA256="$2"; shift 2;;
    --min) MIN="$2"; shift 2;;
    --out) OUT="$2"; shift 2;;
    *) echo "Argument inconnu : $1" >&2; exit 2;;
  esac
done

for req in KEY VERSION URL SHA256; do
  if [[ -z "${!req}" ]]; then echo "Manquant : --${req,,}" >&2; exit 2; fi
done
if [[ ! -f "$KEY" ]]; then echo "Clé introuvable : $KEY" >&2; exit 2; fi

# Normaliser le SHA-256 en minuscules, sans espaces.
SHA256="$(echo "$SHA256" | tr '[:upper:]' '[:lower:]' | tr -d '[:space:]')"

# Payload EXACT (aucun saut de ligne final).
PAYLOAD="$(printf '%s\n%s\n%s\n%s\n%s' "$VERSION" "$CHANNEL" "$URL" "$SHA256" "$MIN")"

SIGNATURE="$(printf '%s' "$PAYLOAD" | openssl dgst -sha256 -sign "$KEY" | openssl base64 -A)"

# Écrire le manifeste (champs attendus par UpdateManifest ; MinimumSupportedVersion omis si vide).
{
  echo "{"
  echo "  \"Version\": \"$VERSION\","
  echo "  \"Channel\": \"$CHANNEL\","
  echo "  \"Url\": \"$URL\","
  echo "  \"Sha256\": \"$SHA256\","
  if [[ -n "$MIN" ]]; then echo "  \"MinimumSupportedVersion\": \"$MIN\","; fi
  echo "  \"Signature\": \"$SIGNATURE\""
  echo "}"
} > "$OUT"

echo "Manifeste signé écrit : $OUT"

# Vérification locale (temp files — évite la substitution de processus, fragile sous git bash Windows).
echo "Vérification locale de la signature :"
TMP_PUB="$(mktemp)"; TMP_SIG="$(mktemp)"; TMP_PAY="$(mktemp)"
trap 'rm -f "$TMP_PUB" "$TMP_SIG" "$TMP_PAY"' EXIT
openssl rsa -in "$KEY" -pubout -out "$TMP_PUB" 2>/dev/null
printf '%s' "$SIGNATURE" | openssl base64 -d -A -out "$TMP_SIG"
printf '%s' "$PAYLOAD" > "$TMP_PAY"
if openssl dgst -sha256 -verify "$TMP_PUB" -signature "$TMP_SIG" "$TMP_PAY" >/dev/null 2>&1; then
  echo "  OK — la signature est valide pour cette clé."
else
  echo "  ÉCHEC — signature invalide (ne pas publier)." >&2
  exit 1
fi
