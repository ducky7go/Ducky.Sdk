#!/bin/bash
set -e

REPO_OWNER="ducky7go"
REPO_NAME="sdk_build_dependency"
TARGET_DIR="./Managed"

echo "Fetching latest release from ${REPO_OWNER}/${REPO_NAME}..."

# Get latest release info from GitHub API
LATEST_RELEASE=$(curl -s "https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/releases/latest")

# Extract download URL for the zipball
DOWNLOAD_URL=$(echo "${LATEST_RELEASE}" | grep -o '"zipball_url": "[^"]*' | cut -d'"' -f4)

if [ -z "$DOWNLOAD_URL" ]; then
    echo "Error: Could not find latest release download URL"
    exit 1
fi

RELEASE_TAG=$(echo "${LATEST_RELEASE}" | grep -o '"tag_name": "[^"]*' | cut -d'"' -f4)
echo "Found latest release: ${RELEASE_TAG}"
echo "Downloading from: ${DOWNLOAD_URL}"

# Download and extract
curl -L -o dependency.zip "${DOWNLOAD_URL}"
echo "Download complete. Extracting..."

unzip -q dependency.zip

# Find the extracted directory (GitHub creates a directory with format owner-repo-commithash)
EXTRACTED_DIR=$(find . -maxdepth 1 -type d -name "${REPO_OWNER}-${REPO_NAME}-*" | head -n 1)

if [ -z "$EXTRACTED_DIR" ]; then
    echo "Error: Could not find extracted directory"
    exit 1
fi

echo "Found extracted directory: ${EXTRACTED_DIR}"

# Remove existing Managed directory if it exists
if [ -d "$TARGET_DIR" ]; then
    echo "Removing existing ${TARGET_DIR} directory..."
    rm -rf "$TARGET_DIR"
fi

# Move Managed folder to target location
if [ -d "${EXTRACTED_DIR}/Managed" ]; then
    mv "${EXTRACTED_DIR}/Managed" "$TARGET_DIR"
    echo "Managed folder moved to ${TARGET_DIR}"
else
    echo "Error: Managed folder not found in ${EXTRACTED_DIR}"
    exit 1
fi

# Cleanup
rm -rf dependency.zip "$EXTRACTED_DIR"
echo "Cleanup complete."
echo "Build dependencies successfully fetched and extracted!"
