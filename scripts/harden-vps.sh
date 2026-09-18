#!/usr/bin/env bash
# Hardens an Ubuntu 24.04 VPS that only needs SSH (22), HTTP (80) and HTTPS (443) from outside. Run it ON the
# server as root; it is idempotent (O6: setup is a script, not a document):
#   scp scripts/harden-vps.sh root@<host>:/root/ && ssh root@<host> "bash /root/harden-vps.sh"
# It does three things: SSH by key only (no passwords), fail2ban for sshd, ufw allowing only 22/80/443.
# It does NOT touch Docker, nginx, the application or any data. The containers' published ports are already
# bound to 127.0.0.1, so ufw does not have to manage them.
set -euo pipefail

[ "$(id -u)" -eq 0 ] || { echo "harden-vps: run as root" >&2; exit 1; }
[ -s /root/.ssh/authorized_keys ] || { echo "harden-vps: REFUSING - /root/.ssh/authorized_keys is empty, disabling passwords would lock you out" >&2; exit 1; }

# 1) SSH: keys only. sshd uses the FIRST value it reads, and Ubuntu's cloud-init drop-in (50-cloud-init.conf)
#    says "PasswordAuthentication yes", so this file has to sort before it - hence the 00- prefix.
cat > /etc/ssh/sshd_config.d/00-hardening.conf <<'CONF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin prohibit-password
CONF
sshd -t                      # refuse to go on if the configuration is invalid
systemctl reload ssh         # existing sessions stay open

# 2) fail2ban: ban an address for 1 hour after 5 failed SSH logins within 10 minutes.
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq fail2ban
cat > /etc/fail2ban/jail.d/sshd.local <<'CONF'
[sshd]
enabled = true
maxretry = 5
findtime = 10m
bantime = 1h
CONF
systemctl enable fail2ban >/dev/null
systemctl restart fail2ban

# 3) Firewall: deny everything inbound except SSH/HTTP/HTTPS. SSH is allowed BEFORE the firewall is enabled.
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

echo
echo "== result =="
sshd -T | grep -E '^(passwordauthentication|permitrootlogin|kbdinteractiveauthentication) '
fail2ban-client status sshd | sed -n '1,4p'
ufw status | sed -n '1,8p'
echo "Open a NEW terminal and confirm you can still log in with your key before closing this session."
