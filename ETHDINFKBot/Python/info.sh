#!/bin/bash

## Rename all files with extension .JPG to .jpg
#find memes -type f -name "*.JPG" -exec sh -c '
#    for file do
#        mv "$file" "${file%.JPG}.jpg"
#    done
#' sh {} +


# Specify the base directory for the search, use "." for the current directory
base_directory="memes"

# Print header
printf "%-20s %-10s %-10s\n" "Extension" "Size" "Count"
printf "%-20s %-10s %-10s\n" "---------" "----" "-----"

# Initialize total size and count variables
total_size=0
total_count=0

# Function to convert size to human-readable format
to_hr() {
    num=$1
    if [ -z "$num" ] || [ "$num" = "0" ]; then
        echo "0B"
    elif [ $num -lt 1024 ]; then 
        echo "${num}B"
    elif [ $num -lt 1048576 ]; then 
        echo "$(echo "scale=2; $num/1024" | bc)K"
    elif [ $num -lt 1073741824 ]; then 
        echo "$(echo "scale=2; $num/1048576" | bc)M"
    else 
        echo "$(echo "scale=2; $num/1073741824" | bc)G"
    fi
}

# Loop through each unique file extension
for ext in $(find "$base_directory" -type f -printf "%f\n" | sed -n 's/.*\.\(.*\)/\1/p' | sort | uniq)
do
    # Find all files with the current extension, calculate total size in bytes and count them
    size_bytes=$(find "$base_directory" -type f -name "*.$ext" -exec du -cb {} + | grep total$ | awk '{print $1}')
    count=$(find "$base_directory" -type f -name "*.$ext" | wc -l)
    
    # Add to total size and count
    total_size=$((total_size + size_bytes))
    total_count=$((total_count + count))

    # Convert size to human-readable format and output in grid format
    size_hr=$(to_hr $size_bytes)
    printf "%-20s %-10s %-10s\n" "$ext" "$size_hr" "$count"
done

# Print a line of dashes
printf "%-20s %-10s %-10s\n" "------------------" "----------" "----------"

# Convert total size to human-readable format and print total row
total_size_hr=$(to_hr $total_size)
printf "%-20s %-10s %-10s\n" "Total" "$total_size_hr" "$total_count"
