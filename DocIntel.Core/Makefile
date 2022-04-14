publish: clean
	sh ../generate-sql-migrations.sh

install:
	mkdir -p $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/upgrade/pgsql/
	mkdir -p $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/install-dbadmin/
	mkdir -p $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/install/
	mkdir -p $(DESTDIR)/usr/share/docintel/
	cp admin-database.sql $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/install-dbadmin/pgsql
	cp ../latest.sql $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/install/pgsql
	cp ../scripts/*.sql $(DESTDIR)/usr/share/dbconfig-common/data/docintel-core/upgrade/pgsql/
	cp ../managed-schema.xml $(DESTDIR)/usr/share/docintel/
	cp ../managed-schema-source $(DESTDIR)/usr/share/docintel/
	cp ../managed-schema-tag $(DESTDIR)/usr/share/docintel/
	cp ../managed-schema-document $(DESTDIR)/usr/share/docintel/
	cp ../solrconfig-document.xml $(DESTDIR)/usr/share/docintel/
	
clean:
	rm -f ../latest.sql
	rm -f ../scripts/*.sql
