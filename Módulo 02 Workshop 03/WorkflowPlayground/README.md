## Exemplo projeto orquestração

Esse projeto simula o processamento de um pedido em um site de ecommerce utilizando orquestração para gerenciar o processo distribuído.

Na pasta SRC, estão os 4 serviços envolvidos na transação.

Para subir as dependências necessárias, inicie o docker e execute o compose com o comando `docker-compose up -d`. Esse compose consiste apenas de uma dependência (SQL Server para persistência)

Após subir as dependências e projetos, acesse o swagger e faça uma request:

`http://localhost:5138/swagger/index.html`