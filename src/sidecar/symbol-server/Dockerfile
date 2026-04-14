FROM nginx:alpine

RUN mkdir -p /symbols /tmp/client_temp /tmp/proxy_temp /tmp/fastcgi_temp /tmp/uwsgi_temp /tmp/scgi_temp

COPY nginx.conf /etc/nginx/nginx.conf

RUN chmod -R 777 /tmp /symbols