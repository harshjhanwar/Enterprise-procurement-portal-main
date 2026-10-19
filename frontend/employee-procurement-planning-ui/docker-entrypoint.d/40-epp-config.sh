#!/bin/sh
set -eu

cat > /usr/share/nginx/html/assets/config.js <<CONFIG
window.__EPP_CONFIG__ = {
  apiBaseUrl: '${EPP_API_BASE_URL:-http://localhost:5080}'
};
CONFIG
