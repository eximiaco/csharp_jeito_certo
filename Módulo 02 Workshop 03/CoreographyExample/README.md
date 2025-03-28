## Exemplo projeto coreografia

Esse projeto simula o processamento de um pedido em um site de ecommerce utilizando coreografia para gerenciar o processo distribuído.

Na pasta SRC, estão os 4 serviços envolvidos na transação.

Para subir as dependências necessárias, inicie o docker e execute o compose com o comando `docker-compose up -d`. Esse compose consiste em duas dependências: SQL Server para persistência e RabbitMQ para troca de mensagens entre os serviços

Após subir as dependências e projetos, faça uma chamada GET para o seguite endereço para iniciar o fluxo:

`curl https://localhost:51864/order`