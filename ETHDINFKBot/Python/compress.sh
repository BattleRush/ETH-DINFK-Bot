#!/bin/bash

# Function to zip files without exceeding the max size limit
zip_files() {
    filetype=$1
    maxsize=$((475 * 1024 * 1024)) # 475 MB in bytes
    zipname=$2
    counter=1
    currentsize=0

    for file in $(find . -name "*.$filetype" -print); do
        filesize=$(stat -c%s "$file")
        newsize=$((currentsize + filesize))

        if [ $newsize -gt $maxsize ]; then
            counter=$((counter + 1))
            currentsize=$filesize
        else
            currentsize=$newsize
        fi

        zip -r "${zipname}_${counter}.zip" "$file"
    done
}

# Compress JPG and JPEG files
zip_files "jpg" "jpg_files"
zip_files "jpeg" "jpeg_files"

# Compress PNG files
zip_files "png" "png_files"
