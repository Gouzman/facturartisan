# Déploiement & secrets (FacturArtisan API)

Objectif: **aucun secret en dur dans le repo**.

## Variables d'environnement (Linux)

La config est lue via `Environment.GetEnvironmentVariable()`:

- `DB_HOST`
- `DB_PORT` (optionnel, défaut Postgres 5432)
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `JWT_KEY` (min 32 caractères recommandé)

Swagger:
- `ENABLE_SWAGGER=true` pour activer Swagger en production (sinon OFF)
- `SWAGGER_ADMIN_EMAILS=email1@x.com,email2@x.com` pour restreindre l'accès Swagger (JWT requis)

Reverse proxy (Nginx):
- `TRUSTED_PROXIES` (optionnel) : liste d'IPs du/des reverse-proxy (séparées par des virgules)
- `TRUSTED_NETWORKS` (optionnel) : liste CIDR des réseaux du proxy (séparées par des virgules)

Exemples:

```bash
export TRUSTED_PROXIES="127.0.0.1"
export TRUSTED_NETWORKS="172.17.0.0/16"
```

## CORS (multi-environment)

En production, la policy `ProductionCors` autorise uniquement:
- `https://facturartisan.online`
- `https://app.facturartisan.online`

En développement, la policy `DevelopmentCors` est permissive (`AllowAnyOrigin`).

Exemple (session shell):

```bash
export DB_HOST="127.0.0.1"
export DB_PORT="5432"
export DB_NAME="facturartisan"
export DB_USER="facturuser"
export DB_PASSWORD="CHANGE_ME"
export JWT_KEY="CHANGE_ME__MIN_32_CHARS________________"
```

## Fallback DEV uniquement (User Secrets)

En `Development`, si les variables ne sont pas définies, l'API peut lire:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`

via **User Secrets** (non commit):

```bash
cd FacturArtisan.Api

dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=facturartisan;Username=facturuser;Password=CHANGE_ME"

dotnet user-secrets set "Jwt:Key" "CHANGE_ME__MIN_32_CHARS________________"
```

## Exemple systemd

Voir le fichier: `deploy/facturartisan-api.service`

Commandes utiles:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now facturartisan-api
sudo systemctl status facturartisan-api --no-pager
sudo journalctl -u facturartisan-api -f
```

## Nginx (headers proxy)

Dans ton vhost Nginx qui fait `proxy_pass` vers Kestrel, assure-toi d'avoir:

```nginx
proxy_set_header Host $host;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
```

Sans ça, le rate limiting IP-based et les URLs/scheme peuvent être faux côté API.
