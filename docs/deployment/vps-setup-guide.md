# Voltflow VPS Setup & Deployment Guide

This guide walks you through setting up both Ubuntu VPS servers:
- **Staging VPS (4GB RAM - 193.164.4.78)**: Connected to the `staging` branch ➔ **https://staging.terminalworks.uk**
- **Production VPS (8GB RAM - 185.169.180.201)**: Connected to the `main` branch ➔ **https://terminalworks.uk**

---

## 1. Initial Server Setup & Security

Connect to your VPS via SSH as root:
```bash
ssh root@YOUR_VPS_IP
```

Update packages:
```bash
apt update && apt upgrade -y
apt install -y curl ufw git unattended-upgrades fail2ban nginx certbot python3-certbot-nginx
```

---

## 2. Install Docker & Docker Compose Plugin

Run the official Docker installation:
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
rm get-docker.sh

# Verify installation
docker --version
docker compose version
```

---

## 3. Create Dedicated Deploy User & SSH Key

```bash
# Create deploy user
adduser --disabled-password --gecos "" deploy
usermod -aG sudo deploy
usermod -aG docker deploy

# Allow passwordless sudo for deploy operations
echo "deploy ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/deploy

# Create application directory
mkdir -p /opt/voltflow
chown -R deploy:deploy /opt/voltflow
```

Set up SSH keys for GitHub Actions:
```bash
# Switch to deploy user
su - deploy

# Create .ssh folder
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# Paste your public SSH key into authorized_keys:
nano ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

> [!TIP]
> **Generate SSH Key Pair on your local PC (PowerShell):**
> ```powershell
> ssh-keygen -t ed25519 -C "github-actions-deploy" -f $HOME/.ssh/id_voltflow_deploy
> ```
> - Copy `id_voltflow_deploy.pub` content into `~/.ssh/authorized_keys` on **both** VPS servers.
> - Private key `id_voltflow_deploy` will be added to GitHub Secrets (`VPS_SSH_KEY`).

---

## 4. Configure UFW Firewall

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw --force enable
sudo ufw status
```

---

## 5. Configure Nginx Reverse Proxy

### On 4GB VPS (Staging):
```bash
# Copy docs/deployment/nginx-staging.conf into:
sudo nano /etc/nginx/sites-available/voltflow.conf

# Enable site
sudo ln -sf /etc/nginx/sites-available/voltflow.conf /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

# Issue Free SSL Certificate with Certbot
sudo certbot --nginx -d staging.terminalworks.uk
```

### On 8GB VPS (Production):
```bash
# Copy docs/deployment/nginx-production.conf into:
sudo nano /etc/nginx/sites-available/voltflow.conf

# Enable site
sudo ln -sf /etc/nginx/sites-available/voltflow.conf /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

# Issue Free SSL Certificate with Certbot
sudo certbot --nginx -d terminalworks.uk -d www.terminalworks.uk
```

*Certbot will automatically obtain certificates, configure HTTPS, and set up automatic renewal.*

---

## 6. Configure GitHub Repository Environments & Secrets

In your GitHub repository, go to **Settings ➔ Environments**:

### 1. `staging` Environment:
Add these secrets:
- `VPS_HOST`: `193.164.4.78` (Staging VPS)
- `VPS_USER`: `deploy`
- `VPS_SSH_KEY`: Content of your private SSH key (`id_voltflow_deploy`)
- `POSTGRES_PASSWORD`: Strong password for staging database
- `VOLT_JWT_KEY`: Random 32+ character key for JWT token signing

### 2. `production` Environment:
Add these secrets:
- `VPS_HOST`: `185.169.180.201` (Production VPS)
- `VPS_USER`: `deploy`
- `VPS_SSH_KEY`: Content of your private SSH key (`id_voltflow_deploy`)
- `POSTGRES_PASSWORD`: Strong password for production database
- `VOLT_JWT_KEY`: Strong, unique random 32+ character key for production

---

## 7. Automated Deployments

| Trigger | Target Environment | Target Server IP | Live URL |
| :--- | :--- | :--- | :--- |
| `git push origin staging` | **Staging** | `193.164.4.78` | **https://staging.terminalworks.uk** |
| `git push origin main` | **Production** | `185.169.180.201` | **https://terminalworks.uk** |
