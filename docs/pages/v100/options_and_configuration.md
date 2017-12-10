---
title: Options and Configuration
sidebar: winnvm_100_sidebar
permalink: v100_options_and_configuration.html
folder: v100/

---

## Options

### Help
To see the help information use `winnvm -h`.

### Installing new NodeJS version.
To install new version of NodeJS use `winnvm -i <version>`.

### Switching Node Versions
To switch to a NodeJs version use `winnvm -u <version>`.

## Configuration
The configurations for winnvm are stored in `%USER_HOME%\.winnvmrc` which is a json format file. Currently only one configuration is the
```json
{
    nodemirror:url of the mirror.
}
```


{% include links.html %}
