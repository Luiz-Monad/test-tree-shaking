 yarn webpack-cli --profile --json | Out-file 'stats.json' -Encoding OEM ; yarn run webpack-bundle-analyzer ((gi stats.json).FullName) 
