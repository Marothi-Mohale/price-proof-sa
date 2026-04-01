FROM postgres:16-alpine

COPY docker/postgres/init-priceproof-db.sh /usr/local/bin/init-priceproof-db.sh

RUN chmod +x /usr/local/bin/init-priceproof-db.sh

ENTRYPOINT ["/usr/local/bin/init-priceproof-db.sh"]
