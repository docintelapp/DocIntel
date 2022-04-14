# Installing DocIntel

1. Add the repository
2. `apt-get update`
3. `apt-get install docintel-all`

## Adding repositories for .NET Core and RabbitMQ

References: 
* https://www.rabbitmq.com/install-debian.html
* https://www.microsoft.com/net/learn/get-started/linux/ubuntu18-04

Download and install the signing key:

    # wget -O - 'https://dl.bintray.com/rabbitmq/Keys/rabbitmq-release-signing-key.asc' | sudo apt-key add -

Update `/etc/apt/sources.list.d/bintray.erlang.list` to include the following:

    deb http://dl.bintray.com/rabbitmq/debian stretch erlang

Update 

    # apt-get update

## Automated install with debconf/ansible

```

- name: Install the database using dbconfig
  debconf:
    name: docintel-core
    question: docintel-core/dbconfig-install
    value: true
    vtype: boolean
    
- name: Host for the PostgreSQL database
  debconf:
    name: docintel-core
    question: docintel-core/remote/host
    value: 'localhost'
    vtype: string

- name: Create Admin user for DocIntel web application
  debconf:
    name: docintel-webapp
    question: docintel-webapp/createuser
    value: true
    vtype: boolean

- name: Admin username
  debconf:
    name: docintel-webapp
    question: docintel-webapp/username
    value: 'admin'
    vtype: string

- name: Admin First Name
  debconf:
    name: docintel-webapp
    question: docintel-webapp/firstname
    value: 'John'
    vtype: string

- name: Admin Last Name
  debconf:
    name: docintel-webapp
    question: docintel-webapp/lastname
    value: 'Doe'
    vtype: string

- name: Admin Email
  debconf:
    name: docintel-webapp
    question: docintel-webapp/email
    value: 'admin@example.org'
    vtype: string

- name: Admin Password
  debconf:
    name: docintel-webapp
    question: docintel-webapp/password
    value: 'dZ!Hs}9XaNN)%68Z'
    vtype: password
  no_log: True
  
```

# Configuration

Create a classification

Add default classification in `appsettings.json`

```
"DefaultClassification": "ed132065-06dc-4608-a0a6-ab68d93f0d1b",
```

# Enable and start all services

Reload systemd configuration files

```
systemctl daemon-reload
```

You can now start the various services

```
systemctl start docintel-document-analyzer
systemctl start docintel-document-indexer
systemctl start docintel-scraper
systemctl start docintel-source-indexer
systemctl start docintel-tag-indexer
systemctl start docintel-thumbnailer
systemctl start docintel-webapp
```

Check the status of the various services to make sure that they all started and are running properly

```
systemctl status docintel-document-analyzer
systemctl status docintel-document-indexer
systemctl status docintel-scraper
systemctl status docintel-source-indexer
systemctl status docintel-tag-indexer
systemctl status docintel-thumbnailer
systemctl status docintel-webapp
```

If all services are running properly, you can enable them to start automatically at boot.

```
systemctl enable docintel-document-analyzer
systemctl enable docintel-document-indexer
systemctl enable docintel-scraper
systemctl enable docintel-source-indexer
systemctl enable docintel-tag-indexer
systemctl enable docintel-thumbnailer
systemctl enable docintel-webapp
```
    
### Install nginx

- `apt-get install nginx`
  (you might need to adjust firewall: https://www.digitalocean.com/community/tutorials/how-to-install-nginx-on-ubuntu-18-04)
- Configure nginx to run .NET CORE applications
  https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.1&tabs=aspnetcore2x

Example file `/etc/nginx/sites-enabled/docint`
```
server {
    listen       443;
    server_name  docint.mycompany.com;
    access_log   /var/log/nginx/docint01.access.log;
    error_log    /var/log/nginx/docint01.error.log;

    ssl                  on;
    ssl_certificate      /etc/ssl/private/docint.crt;
    ssl_certificate_key  /etc/ssl/private/docint.key;
    ssl_session_timeout  5m;

    location / {
        proxy_pass         http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_pass_header  Server;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        client_max_body_size 5M;
    }
}
```

Reload the configuration

```
nginx -s reload
```

# Get list of all dependencies

    cat **/*.csproj | grep "PackageReference" | grep "Include" | awk '{$1=$1;print}' | sed "s/<PackageReference Include=\"//g" | sed "s/\" Version=\"/, /g" | sed "s/\" \/>//g" | awk '{$1=$1;print}' | sort | uniq

