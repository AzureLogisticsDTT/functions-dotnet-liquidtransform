

SUBS?=44685035-1fa7-4806-ad41-1728fa06995d
RG?=AzureLogistics-dev
T?=parameters.json


all:
		az account set -s $(SUBS)
		az deployment group create --resource-group $(RG) --template-file template.json --parameters $(T)
deploy: 
		az deployment group create --resource-group $(RG) --template-file template.json --parameters $(T)