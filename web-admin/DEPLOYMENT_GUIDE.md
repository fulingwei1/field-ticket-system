# Web管理端部署和远程访问指南

本指南将帮助你运行Web管理端，并让远程客服工程师能够访问系统。

## 📦 前置条件

### 系统要求
- Node.js >= 16.x
- npm >= 8.x 或 yarn >= 1.22

### 检查Node.js版本
```bash
node --version  # 应该 >= 16.x
npm --version   # 应该 >= 8.x
```

如果没有安装Node.js，请访问：https://nodejs.org/

## 🚀 本地开发运行

### 1. 安装依赖

在 `web-admin` 目录下运行：

```bash
cd /home/user/field-ticket-system/web-admin
npm install
```

### 2. 配置环境变量（可选）

创建 `.env.local` 文件（如果需要自定义配置）：

```bash
# API服务器地址
VITE_API_URL=http://localhost:5000

# 如果需要指定其他后端地址
# VITE_API_URL=http://192.168.1.100:5000
```

### 3. 启动开发服务器

```bash
npm run dev
```

服务将在 http://localhost:5173 启动

**重要提示：**
- Vite配置了 `host: true`，这意味着可以通过你的局域网IP访问
- 访问地址：`http://你的IP:5173`
- 查看你的IP：`ip addr` 或 `ifconfig`

### 4. 构建生产版本

```bash
npm run build
```

构建产物在 `dist` 目录

### 5. 预览生产版本

```bash
npm run preview
```

## 🌐 远程访问方案

根据你的需求，有以下几种方案：

---

## 方案1：局域网访问（推荐，最简单）

**适用场景：** 客服工程师和服务器在同一局域网（公司内网）

### 步骤：

1. **启动服务**
```bash
cd /home/user/field-ticket-system/web-admin
npm run dev
```

2. **查看服务器IP地址**
```bash
# Linux
ip addr show | grep inet

# 或者
hostname -I

# 你会看到类似：192.168.1.100
```

3. **客服工程师访问**
在浏览器中打开：`http://服务器IP:5173`

例如：`http://192.168.1.100:5173`

### 注意事项：
- ✅ 确保防火墙开放5173端口
- ✅ 确保后端API服务（5000端口）也在运行

---

## 方案2：使用Nginx反向代理（生产推荐）

**适用场景：** 生产环境部署，更稳定可靠

### 步骤：

#### 1. 安装Nginx
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install nginx

# CentOS/RHEL
sudo yum install nginx
```

#### 2. 构建前端
```bash
cd /home/user/field-ticket-system/web-admin
npm run build
```

#### 3. 配置Nginx

创建配置文件 `/etc/nginx/sites-available/field-ticket`：

```nginx
server {
    listen 80;
    server_name your-domain.com;  # 改为你的域名或IP

    # 前端静态文件
    root /home/user/field-ticket-system/web-admin/dist;
    index index.html;

    # 前端路由支持
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API代理到后端
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_cache_bypass $http_upgrade;
    }

    # Gzip压缩
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
}
```

#### 4. 启用配置
```bash
sudo ln -s /etc/nginx/sites-available/field-ticket /etc/nginx/sites-enabled/
sudo nginx -t  # 测试配置
sudo systemctl restart nginx
```

#### 5. 访问
客服工程师访问：`http://服务器IP`

---

## 方案3：内网穿透（适合远程办公）

**适用场景：** 客服工程师不在同一局域网，需要通过互联网访问

### 选项A：使用frp（免费，自建）

#### 服务器端配置：

1. **下载frp**
```bash
wget https://github.com/fatedier/frp/releases/download/v0.52.0/frp_0.52.0_linux_amd64.tar.gz
tar -xzf frp_0.52.0_linux_amd64.tar.gz
cd frp_0.52.0_linux_amd64
```

2. **配置frpc.ini（客户端）**
```ini
[common]
server_addr = your-vps-ip  # 你的公网服务器IP
server_port = 7000

[web-admin]
type = http
local_port = 5173
custom_domains = field-ticket.yourdomain.com
```

3. **启动frp客户端**
```bash
./frpc -c frpc.ini
```

### 选项B：使用ngrok（简单，付费）

1. **安装ngrok**
```bash
# 访问 https://ngrok.com/ 注册账号
# 下载并安装ngrok

# 认证
ngrok authtoken YOUR_AUTH_TOKEN
```

2. **启动隧道**
```bash
ngrok http 5173
```

3. **获取公网地址**
ngrok会提供一个地址，如：`https://abc123.ngrok.io`

### 选项C：使用Cloudflare Tunnel（免费，推荐）

1. **安装cloudflared**
```bash
# Ubuntu/Debian
wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb
```

2. **登录Cloudflare**
```bash
cloudflared tunnel login
```

3. **创建隧道**
```bash
cloudflared tunnel create field-ticket
```

4. **配置隧道**
创建 `~/.cloudflared/config.yml`：
```yaml
tunnel: <tunnel-id>
credentials-file: /home/user/.cloudflared/<tunnel-id>.json

ingress:
  - hostname: field-ticket.yourdomain.com
    service: http://localhost:5173
  - service: http_status:404
```

5. **启动隧道**
```bash
cloudflared tunnel run field-ticket
```

---

## 方案4：VPN方案（企业级，最安全）

**适用场景：** 对安全性要求高的企业环境

### 使用WireGuard VPN

#### 1. 安装WireGuard
```bash
sudo apt update
sudo apt install wireguard
```

#### 2. 生成密钥
```bash
wg genkey | tee privatekey | wg pubkey > publickey
```

#### 3. 配置服务器
编辑 `/etc/wireguard/wg0.conf`：
```ini
[Interface]
PrivateKey = <服务器私钥>
Address = 10.0.0.1/24
ListenPort = 51820

[Peer]
PublicKey = <客户端公钥>
AllowedIPs = 10.0.0.2/32
```

#### 4. 启动VPN
```bash
sudo wg-quick up wg0
sudo systemctl enable wg-quick@wg0
```

#### 5. 客服工程师配置
提供客户端配置文件，连接VPN后访问：`http://10.0.0.1:5173`

---

## 🔒 安全建议

### 1. 防火墙配置

**开放必要端口：**
```bash
# UFW (Ubuntu)
sudo ufw allow 5173/tcp  # 开发服务器
sudo ufw allow 80/tcp    # Nginx HTTP
sudo ufw allow 443/tcp   # Nginx HTTPS

# firewalld (CentOS)
sudo firewall-cmd --permanent --add-port=5173/tcp
sudo firewall-cmd --reload
```

### 2. 启用HTTPS（生产环境必须）

**使用Let's Encrypt免费证书：**
```bash
# 安装certbot
sudo apt install certbot python3-certbot-nginx

# 获取证书
sudo certbot --nginx -d your-domain.com

# 自动续期
sudo certbot renew --dry-run
```

### 3. 访问控制

在Nginx配置中添加IP白名单：
```nginx
location / {
    allow 192.168.1.0/24;  # 允许内网
    allow 1.2.3.4;         # 允许特定公网IP
    deny all;              # 拒绝其他

    try_files $uri $uri/ /index.html;
}
```

### 4. 设置基本认证（可选）
```bash
# 安装htpasswd
sudo apt install apache2-utils

# 创建用户
sudo htpasswd -c /etc/nginx/.htpasswd admin

# Nginx配置
location / {
    auth_basic "Restricted Access";
    auth_basic_user_file /etc/nginx/.htpasswd;
    # ...
}
```

---

## 📊 推荐方案对比

| 方案 | 难度 | 成本 | 安全性 | 适用场景 |
|------|------|------|--------|----------|
| 局域网访问 | ⭐ | 免费 | ⭐⭐⭐ | 同一内网 |
| Nginx反向代理 | ⭐⭐ | 免费 | ⭐⭐⭐⭐ | 生产环境 |
| frp内网穿透 | ⭐⭐⭐ | 需VPS | ⭐⭐ | 远程访问 |
| Cloudflare Tunnel | ⭐⭐ | 免费 | ⭐⭐⭐⭐ | 远程访问 |
| VPN | ⭐⭐⭐⭐ | 免费-付费 | ⭐⭐⭐⭐⭐ | 企业环境 |

---

## 🎯 我的推荐

### 场景1：开发测试（局域网内）
```bash
# 最简单
cd web-admin
npm install
npm run dev
# 访问：http://服务器IP:5173
```

### 场景2：正式生产（公司内网）
```bash
# Nginx + 构建版本
npm run build
# 配置Nginx反向代理
# 访问：http://服务器IP
```

### 场景3：远程访问（互联网）
```bash
# Cloudflare Tunnel（免费且安全）
cloudflared tunnel run field-ticket
# 访问：https://field-ticket.yourdomain.com
```

---

## 🐛 常见问题

### Q1: npm install 失败
```bash
# 清除缓存
npm cache clean --force
rm -rf node_modules package-lock.json
npm install
```

### Q2: 端口被占用
```bash
# 查看端口占用
sudo lsof -i :5173
# 或者修改端口
VITE_PORT=3000 npm run dev
```

### Q3: API请求失败（跨域）
确保后端API也在运行，并检查Vite配置的代理设置。

### Q4: 防火墙阻止访问
```bash
# 临时关闭防火墙测试
sudo ufw disable
# 或开放端口
sudo ufw allow 5173/tcp
```

### Q5: 构建后白屏
检查路由配置，确保使用 `BrowserRouter` 而非 `HashRouter`

---

## 📞 联系支持

如遇到问题，请提供：
1. 错误信息截图
2. 浏览器控制台日志
3. 网络环境（局域网/公网）
4. 部署方式

---

## 🔄 更新部署

### 拉取最新代码
```bash
cd /home/user/field-ticket-system
git pull origin main
```

### 更新前端
```bash
cd web-admin
npm install  # 如果依赖有更新
npm run build
sudo systemctl restart nginx  # 如果使用Nginx
```

---

## 📝 系统服务配置（可选）

创建systemd服务，让Web服务开机自启：

### 开发服务（仅用于开发）
创建 `/etc/systemd/system/field-ticket-web.service`：
```ini
[Unit]
Description=Field Ticket Web Admin
After=network.target

[Service]
Type=simple
User=your-username
WorkingDirectory=/home/user/field-ticket-system/web-admin
ExecStart=/usr/bin/npm run dev
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

启用服务：
```bash
sudo systemctl daemon-reload
sudo systemctl enable field-ticket-web
sudo systemctl start field-ticket-web
```

---

**祝部署顺利！** 🎉
