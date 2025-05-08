## Exemplo projeto vertical slice + testes de integração

Esse projeto simula um sistema de lançamentos bancários.

Para subir as dependências necessárias, inicie o docker e execute o compose com o comando `docker-compose up -d`. Esse compose consiste de 3 dependências (SqlServer, RabbitMq e seq).

Após subir as dependências e projetos, rode o projeto e utilize o arquivo webapi.http para testar os endpoints.

É necessário a criação de um usuário para obter o bearer token e começar a utilizar os endpoints.

Os endpoints disponíveis podem ser acessados também através da seguinte url:

https://localhost:7008/scalar/