# DocIntel

DocIntel is an open-source centralized knowledge base for your threat
intelligence. It makes information available to all team members, facilitate
searches, and encourage collaboration. It replaces analyst’s folders ‘Reports’
and cybersecurity vendors information portals.

Organize your threat reports, leverage intelligence and empower your threat
intelligence team :-)

* Website: https://docintel.org/
* Docker images: https://hub.docker.com/orgs/docintelapp
* Slack: https://docintelapp.slack.com/

## Installation

A run.sh script is available to help you install the application as easily as possible. The script will ask you a few questions and will create a docker-compose.yml file for you.

    curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/run.sh -o run.sh
    sh run.sh
  
Check the configurations and the compose file. You can then run the deployment with

    docker-compose -f docker-compose.yml -p docintel-dev up -d
  
However, you would not be able to log in the platform. You need to create an account. We recommend creating a specific admin account that you don't use for your daily tasks.

    docker exec -it docintel-dev-webapp \
      dotnet /cli/DocIntel.AdminConsole.dll \
      user add --username admin
    docker exec -it docintel-dev-webapp \
      dotnet /cli/DocIntel.AdminConsole.dll \
      user role --username admin --role administrator
  
You can now login on http://localhost:5005.
