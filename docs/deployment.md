# Deployment

FlipZA's Engine runs as a single Docker container on an Ubuntu VPS. It opens **no ports**: it only makes outgoing requests to Takealot and writes to a SQLite file on a mounted folder.

## Requirements

| | Minimum | Current VPS |
|---|---|---|
| OS | Linux with Docker | Ubuntu 24.04 LTS |
| RAM | 1 GB to run; ~2 GB (or swap) to build the image | 1.9 GB + 2 GB swap |
| Disk | ~2 GB for images + database growth | 73 GB |

## How the container is built

`src/FlipZA.Engine/Dockerfile` is a two-stage build:

1. **Build stage** (`mcr.microsoft.com/dotnet/sdk:9.0`): restores packages (cached separately from the source code) and publishes the Engine in Release mode.
2. **Runtime stage** (`mcr.microsoft.com/dotnet/runtime:9.0`): copies the published app (~43 MB) and runs it as the non-root `app` user (uid **1654**), with `DOTNET_ENVIRONMENT=Production` and the database at `/data/flipza.db`.

`docker-compose.yml` adds:
- `restart: unless-stopped`: restarts after crashes and reboots;
- `./data:/data`: the database lives on the host in `/opt/flipza/data`, so it survives redeploys;
- `TZ=Africa/Johannesburg`: local times in logs;
- log rotation: 5 files of 10 MB.

---

## First-time setup

```bash
# 1. Update the system and install Docker
sudo apt update && sudo apt upgrade -y
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER          # log out and back in afterwards

# 2. Check Takealot accepts requests from this server (expect 200)
curl -s -o /dev/null -w "%{http_code}\n" -A "FlipZA/0.1" \
  "https://api.takealot.com/rest/v-1-16-0/product-details/PLID98945485?platform=desktop"

# 3. Get the code
sudo mkdir -p /opt/flipza && sudo chown $USER /opt/flipza
git clone https://github.com/simseza022/FlipZA.git /opt/flipza
cd /opt/flipza

# 4. Create the data folder, owned by the container user
mkdir -p data && sudo chown 1654:1654 data

# 5. Build and start
docker compose up -d --build
docker compose logs -f
```

If the repository is private, clone with a read-only **deploy key** (GitHub → repo Settings → Deploy keys) and `git@github.com:simseza022/FlipZA.git`.

### What healthy logs look like

```text
Loaded robots.txt for https://api.takealot.com
Search 'power bank' returned 72 products
...
Takealot discovery complete: 1440 search hits, 150 tracked (150 new, 150 saved, 0 failed), 0 failed searches
Takealot refresh complete: 150/150 saved, 0 delisted, 0 failed. Next run at: ...
```

## Updating

```bash
cd /opt/flipza
git pull
docker compose up -d --build
```

Database migrations run automatically on startup. Existing data is kept.

## Everyday commands

| Task | Command |
|---|---|
| Follow logs | `docker compose logs -f` |
| Last 200 log lines | `docker compose logs --tail 200` |
| Status | `docker compose ps` |
| Restart | `docker compose restart` |
| Stop | `docker compose down` |
| Memory/CPU use | `docker stats` |

---

## Backups

SQLite must be copied with its `.backup` command (a plain file copy of a database being written can be corrupt). Run it as uid 1654; see the warning below.

```bash
sudo apt install -y sqlite3
sudo mkdir -p /opt/flipza/backups && sudo chown 1654:1654 /opt/flipza/backups
sudo crontab -e
```

Add this line for a daily 03:00 backup, keeping 7 days:

```text
0 3 * * * sudo -u '#1654' sqlite3 /opt/flipza/data/flipza.db ".backup /opt/flipza/backups/flipza-$(date +\%F).db" && find /opt/flipza/backups -name 'flipza-*.db' -mtime +7 -delete
```

To restore a backup:

```bash
cd /opt/flipza
docker compose down
sudo rm -f data/flipza.db-wal data/flipza.db-shm
sudo cp backups/flipza-2026-10-20.db data/flipza.db && sudo chown 1654:1654 data/flipza.db
docker compose up -d
```

---

## Viewing the data

> **Always open the database as uid 1654, never as root.** In WAL mode SQLite creates `flipza.db-wal` and `flipza.db-shm` next to the database. If root creates them, the container can no longer write and fails with *"attempt to write a readonly database"*. Fix: `sudo chown 1654:1654 /opt/flipza/data/*`.

### On the server (sqlite3)

```bash
cd /opt/flipza
sudo -u '#1654' sqlite3 -readonly -header -column data/flipza.db
```

Useful prompt commands: `.tables`, `.schema ListingSnapshots`, `.quit`. Ready-made queries are in [Data model](data-model.md#useful-queries).

### On your laptop (DataGrip or DB Browser for SQLite)

SQLite is a file, so a desktop tool needs a local copy. Do **not** mount the server folder over the network (sshfs etc.): WAL mode does not work across network file systems and can corrupt the live database.

Save as `~/bin/flipza-pull-db.sh` on your laptop:

```bash
#!/usr/bin/env bash
set -euo pipefail
VPS=sisko@<your-vps-ip>

ssh -t "$VPS" "sudo -u '#1654' sqlite3 /opt/flipza/data/flipza.db '.backup /opt/flipza/backups/latest.db'"
scp "$VPS:/opt/flipza/backups/latest.db" ~/flipza-latest.db
echo "Updated ~/flipza-latest.db"
```

Then in DataGrip: **New → Data Source → SQLite**, file `~/flipza-latest.db`, tick **Read-only**. Before pulling a fresh copy, **disconnect** the data source; reconnect afterwards.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `no configuration file provided` | `docker-compose.yml` not on the server | `git pull` in `/opt/flipza` |
| `attempt to write a readonly database` / permission denied on `/data` | Data folder or WAL files not owned by uid 1654 | `sudo chown -R 1654:1654 /opt/flipza/data` |
| Build killed / out of memory | Not enough RAM for the SDK build | Add swap, or build on another machine and copy the image |
| Many `403` errors | Takealot blocking the server's IP | Test with the `curl` above; raise `MinRequestIntervalMs`; contact Takealot |
| Every request `404` | Takealot retired the API version | Update `Takealot__ApiVersion` |
| `Critical: Every Takealot product failed` | API or response format changed | Check logs and `LastRawPayload`; see [Takealot scraper](scraper.md#when-takealot-changes-something) |
| `RobotsDisallowedException` | Takealot's robots.txt now forbids that path | Respect it; change what is scraped |
