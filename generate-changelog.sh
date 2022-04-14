#!/bin/sh
# 

set -e

if [ -d "debian" ]; then
    > debian/changelog
    echo "-- Updating changelog --"
    current_dir=$(pwd)
    prevtag=initial
    pkgname=`cat $current_dir/debian/control | grep '^Package: ' | sed 's/^Package: //'`

	cd ..

    git tag -l v* | sort -V | while read tag; do
        (echo "$pkgname (${tag#v}) unstable; urgency=low\n"; git log --pretty=format:'  * %s' $prevtag..$tag -- . | uniq ; echo "\n"; (git log --pretty='format:%n%n -- Antoine Cailliau <antoine.cailliau@mil.be>  %aD%n%n' $tag^..$tag) | grep "\S" | head -n 1; echo "\n";) | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}' | cat - $current_dir/debian/changelog | sponge $current_dir/debian/changelog
        prevtag=$tag
    done

    last_tag=`git tag -l v* | sort -V | tail -1`
    git log --reverse --pretty=format:'%h' $last_tag..HEAD . | while read commit; do
        (echo "$pkgname (`cat VERSION`.`git log --pretty=format:'%at' -n 1 $commit .`+dev.`git log --pretty=format:'%h' -n 1 $commit .`) experimental; urgency=low\n"; git log --pretty=format:'  * %s' -n 1 $commit; git log --pretty='format:%n%n -- Antoine Cailliau <antoine.cailliau@mil.be>  %aD%n%n' $commit^..$commit) | cat - $current_dir/debian/changelog | sponge $current_dir/debian/changelog
    done
fi

