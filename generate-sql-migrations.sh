#!/bin/sh
# 

set -e

cd $(dirname $0)

rm -rf scripts
mkdir -p scripts

cd DocIntel.WebApp
dotnet build
cd ..

cd DocIntel.Core

# Generate scripts for migrations from tag vX.X.X
prevtag=initial
git tag -l v* | sort -V | while read tag || [ -n "$tag" ]; do
    version="${tag#v}"
    from=`git ls-tree -r --name-only $prevtag ../DocIntel.Core/Migrations | grep -P '[0-9]{14}_[^\.]+' -o | sort -r | head -n1`
    to=`git ls-tree -r --name-only $tag ../DocIntel.Core/Migrations  | grep -P '[0-9]{14}_[^\.]+' -o | sort -r | head -n1`
    if [ ! -z "$to" ]; then    
        if [ -z "$from" ]; then
	        echo "Migrate to '$to'."	        
	        cd ../DocIntel.WebApp
	        $HOME/.dotnet/tools/dotnet-ef migrations script -i --no-transactions --no-build --project=../DocIntel.Core "0" "$to" -o ../scripts/$version.sql
	        cd ../DocIntel.Core
	    else
	        if [ "$from" != "$to" ]; then
    	        echo "Migrate from '$from' to '$to'."
	            cd ../DocIntel.WebApp
	            $HOME/.dotnet/tools/dotnet-ef migrations script -i --no-transactions --no-build --project=../DocIntel.Core "$from" "$to" -o ../scripts/$version.sql
	            cd ../DocIntel.Core
       	    else
        	    echo "No migration"
    	    fi    	    
	    fi
    fi
    prevtag=$tag
done

# Generate scripts for migrations from commit to commit (i.e. devel versions)
# git log --pretty=format:'%h' ./Migrations | while read commit || [ -n "$commit" ]; do
#     version=`git show $commit:VERSION 2>/dev/null || echo "1.0.0"`.`git log --pretty=format:'%ct' -n 1 $commit .`+dev.`git log --pretty=format:'%h' -n 1 $commit .`  
#     from=`git ls-tree -r --name-only $commit^1 ../DocIntel.Core/Migrations | grep -P '[0-9]{14}_[^\.]+' -o | sort -r | head -n1`
#     to=`git ls-tree -r --name-only $commit ../DocIntel.Core/Migrations  | grep -P '[0-9]{14}_[^\.]+' -o | sort -r | head -n1`
#         
#     if [ ! -z "$to" ]; then    
#         if [ -z "$from" ]; then
# 	        echo "Migrate to '$to'."	        
# 	        cd ../DocIntel.WebApp
# 	        $HOME/.dotnet/tools/dotnet-ef migrations script -i --no-transactions --no-build --project=../DocIntel.Core "0" "$to" -o ../scripts/$version.sql
# 	        cd ../DocIntel.Core
# 	    else
# 	        if [ "$from" != "$to" ]; then
#     	        echo "Migrate from '$from' to '$to'."
# 	            cd ../DocIntel.WebApp
# 	            $HOME/.dotnet/tools/dotnet-ef migrations script -i --no-transactions --no-build --project=../DocIntel.Core "$from" "$to" -o ../scripts/$version.sql
# 	            cd ../DocIntel.Core
#        	    else
#         	    echo "No migration"
#     	    fi    	    
# 	    fi
#     fi
# done


cd ../DocIntel.WebApp
$HOME/.dotnet/tools/dotnet-ef migrations script -i --no-transactions --no-build --project=../DocIntel.Core -o ../latest.sql
cd ../DocIntel.Core

cd ..

exit
