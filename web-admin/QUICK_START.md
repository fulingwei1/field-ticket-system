# 🚀 Web管理端快速启动指南

## 一、本地启动（最简单）

### 方式1：使用启动脚本（推荐）

```bash
cd /home/user/field-ticket-system/web-admin
./start.sh
```

就这么简单！脚本会自动检查并安装依赖，然后启动开发服务器。

### 方式2：手动启动

```bash
cd /home/user/field-ticket-system/web-admin
npm install    # 首次运行
npm run dev    # 启动开发服务器
```

### 访问地址

- **本地访问**: http://localhost:5173
- **局域网访问**: http://你的IP:5173

查看你的IP地址：
```bash
# Linux
hostname -I

# 或
ip addr | grep 'inet '
```

---

## 二、远程访问配置

### 场景1：同一局域网（公司内网）

**客服工程师可以直接访问** ✅

1. 启动服务：`./start.sh`
2. 查看服务器IP：`hostname -I`
3. 客服访问：`http://服务器IP:5173`

**注意事项：**
- 确保防火墙开放5173端口
- 后端API也需要运行（5000端口）

---

### 场景2：不同网络（远程办公）

#### 选项A：Cloudflare Tunnel（免费，推荐）

**优点：** 免费、安全、不需要公网IP

```bash
# 1. 安装 cloudflared
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb

# 2. 登录Cloudflare
cloudflared tunnel login

# 3. 创建隧道
cloudflared tunnel create field-ticket

# 4. 运行隧道
cloudflared tunnel --url http://localhost:5173
```

客服工程师可通过 Cloudflare 提供的地址访问。

#### 选项B：frp内网穿透

**前提：** 需要有一台公网服务器

配置frpc.ini：
```ini
[common]
server_addr = 你的公网服务器IP
server_port = 7000

[web]
type = http
local_port = 5173
custom_domains = field.yourdomain.com
```

启动：
```bash
./frpc -c frpc.ini
```

#### 选项C：ngrok（最简单，但要付费）

```bash
# 1. 安装 ngrok
# 访问 https://ngrok.com/ 注册

# 2. 启动
ngrok http 5173
```

ngrok 会提供一个公网地址。

---

## 三、生产部署（Nginx）

### 快速部署

```bash
# 1. 构建
cd /home/user/field-ticket-system/web-admin
./start.sh build

# 2. 安装Nginx
sudo apt install nginx

# 3. 配置Nginx
sudo nano /etc/nginx/sites-available/field-ticket
```

粘贴配置：
```nginx
server {
    listen 80;
    server_name _;

    root /home/user/field-ticket-system/web-admin/dist;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
    }
}
```

启用并重启：
```bash
sudo ln -s /etc/nginx/sites-available/field-ticket /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

访问：`http://服务器IP`

---

## 四、防火墙配置

### 开放端口

```bash
# Ubuntu/Debian (UFW)
sudo ufw allow 5173/tcp   # 开发服务器
sudo ufw allow 80/tcp     # Nginx HTTP
sudo ufw allow 443/tcp    # Nginx HTTPS

# CentOS/RHEL (firewalld)
sudo firewall-cmd --permanent --add-port=5173/tcp
sudo firewall-cmd --permanent --add-port=80/tcp
sudo firewall-cmd --reload
```

### 检查端口是否开放

```bash
# 检查端口监听
sudo netstat -tlnp | grep :5173

# 或使用 ss
sudo ss -tlnp | grep :5173
```

---

## 五、常见问题速查

### ❌ 无法访问？

1. **检查服务是否运行**
   ```bash
   # 查看进程
   ps aux | grep vite
   ```

2. **检查端口**
   ```bash
   sudo netstat -tlnp | grep 5173
   ```

3. **检查防火墙**
   ```bash
   sudo ufw status
   # 临时关闭测试
   sudo ufw disable
   ```

4. **检查后端API**
   ```bash
   curl http://localhost:5000/api/health
   ```

### ❌ npm install 失败？

```bash
# 清除缓存
npm cache clean --force
rm -rf node_modules package-lock.json
npm install
```

### ❌ 端口被占用？

```bash
# 查看占用
sudo lsof -i :5173

# 杀死进程
kill -9 <PID>

# 或换个端口
VITE_PORT=3000 npm run dev
```

### ❌ API请求跨域？

确保：
1. 后端服务运行中（localhost:5000）
2. vite.config.ts 配置了正确的代理
3. 浏览器控制台查看具体错误

---

## 六、推荐方案

| 场景 | 方案 | 命令 |
|------|------|------|
| 本地开发 | 直接启动 | `./start.sh` |
| 局域网访问 | 直接启动 | `./start.sh` → 访问 `http://IP:5173` |
| 生产部署 | Nginx | `./start.sh build` + Nginx配置 |
| 远程访问 | Cloudflare Tunnel | `cloudflared tunnel --url localhost:5173` |

---

## 七、系统要求

- **Node.js**: >= 16.x
- **npm**: >= 8.x
- **操作系统**: Linux / macOS / Windows
- **浏览器**: Chrome / Firefox / Safari / Edge (最新版本)

检查版本：
```bash
node --version
npm --version
```

---

## 📞 需要帮助？

1. 查看完整文档：[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
2. 查看项目文档：[README.md](./README.md)
3. 联系开发团队

---

## 🎯 最快速启动（TL;DR）

```bash
# 1. 进入目录
cd /home/user/field-ticket-system/web-admin

# 2. 一键启动
./start.sh

# 3. 打开浏览器
# http://localhost:5173
```

**就这么简单！** 🚀
