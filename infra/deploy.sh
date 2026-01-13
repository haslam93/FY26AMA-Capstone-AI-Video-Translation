#!/bin/bash
# ============================================================================
# Video Translation Service - Infrastructure Deployment Script
# ============================================================================
# This script deploys all Azure infrastructure for the video translation service
# Prerequisites: Azure CLI installed and logged in
# ============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
DEPLOYMENT_NUMBER=""
LOCATION="eastus2"
WHAT_IF=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--number)
            DEPLOYMENT_NUMBER="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        --what-if)
            WHAT_IF=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./deploy.sh -n <deployment-number> [-l <location>] [--what-if]"
            echo ""
            echo "Options:"
            echo "  -n, --number    Deployment number (required, 1-999)"
            echo "  -l, --location  Azure region (default: eastus2)"
            echo "  --what-if       Run what-if analysis without deploying"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$DEPLOYMENT_NUMBER" ]; then
    echo -e "${RED}Error: Deployment number is required${NC}"
    echo "Usage: ./deploy.sh -n <deployment-number>"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE_FILE="$SCRIPT_DIR/main.bicep"
RESOURCE_GROUP_NAME="AMAFY26-deployment-$DEPLOYMENT_NUMBER"

echo -e "${CYAN}=============================================${NC}"
echo -e "${CYAN}Video Translation Service - Infrastructure${NC}"
echo -e "${CYAN}=============================================${NC}"
echo ""
echo -e "${YELLOW}Deployment Number: $DEPLOYMENT_NUMBER${NC}"
echo -e "${YELLOW}Location: $LOCATION${NC}"
echo -e "${YELLOW}Resource Group: $RESOURCE_GROUP_NAME${NC}"
echo ""

# Check Azure CLI login
echo -e "Checking Azure CLI login status..."
if ! az account show > /dev/null 2>&1; then
    echo -e "${RED}Please login to Azure CLI first: az login${NC}"
    exit 1
fi

ACCOUNT_NAME=$(az account show --query "user.name" -o tsv)
SUBSCRIPTION_NAME=$(az account show --query "name" -o tsv)
echo -e "${GREEN}Logged in as: $ACCOUNT_NAME${NC}"
echo -e "${GREEN}Subscription: $SUBSCRIPTION_NAME${NC}"
echo ""

# Validate the template
echo "Validating Bicep template..."
if ! az deployment sub validate \
    --location "$LOCATION" \
    --template-file "$TEMPLATE_FILE" \
    --parameters deploymentNumber="$DEPLOYMENT_NUMBER" location="$LOCATION" \
    > /dev/null 2>&1; then
    echo -e "${RED}Template validation failed${NC}"
    az deployment sub validate \
        --location "$LOCATION" \
        --template-file "$TEMPLATE_FILE" \
        --parameters deploymentNumber="$DEPLOYMENT_NUMBER" location="$LOCATION"
    exit 1
fi
echo -e "${GREEN}Template validation successful!${NC}"
echo ""

# What-If deployment
if [ "$WHAT_IF" = true ]; then
    echo "Running What-If analysis..."
    az deployment sub what-if \
        --location "$LOCATION" \
        --template-file "$TEMPLATE_FILE" \
        --parameters deploymentNumber="$DEPLOYMENT_NUMBER" location="$LOCATION"
    exit 0
fi

# Confirm deployment
read -p "Do you want to proceed with the deployment? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Deployment cancelled.${NC}"
    exit 0
fi

# Deploy
echo ""
echo "Starting deployment..."
DEPLOYMENT_NAME="VideoTranslation-$(date +%Y%m%d-%H%M%S)"

RESULT=$(az deployment sub create \
    --name "$DEPLOYMENT_NAME" \
    --location "$LOCATION" \
    --template-file "$TEMPLATE_FILE" \
    --parameters deploymentNumber="$DEPLOYMENT_NUMBER" location="$LOCATION" \
    --output json)

if [ $? -ne 0 ]; then
    echo -e "${RED}Deployment failed!${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}=============================================${NC}"
echo -e "${GREEN}Deployment Successful!${NC}"
echo -e "${GREEN}=============================================${NC}"
echo ""
echo -e "${YELLOW}Outputs:${NC}"
echo "  Resource Group: $(echo $RESULT | jq -r '.properties.outputs.resourceGroupName.value')"
echo "  Function App: $(echo $RESULT | jq -r '.properties.outputs.functionAppName.value')"
echo "  Function App URL: https://$(echo $RESULT | jq -r '.properties.outputs.functionAppHostname.value')"
echo "  Speech Endpoint: $(echo $RESULT | jq -r '.properties.outputs.speechServiceEndpoint.value')"
echo "  AI Foundry Endpoint: $(echo $RESULT | jq -r '.properties.outputs.aiFoundryEndpoint.value')"
echo "  Key Vault URI: $(echo $RESULT | jq -r '.properties.outputs.keyVaultUri.value')"
echo "  Storage Account: $(echo $RESULT | jq -r '.properties.outputs.storageAccountName.value')"
echo ""
echo -e "${CYAN}Next Steps:${NC}"
echo "  1. Deploy the Function App code"
echo "  2. Configure any additional app settings"
echo "  3. Test the video translation workflow"
