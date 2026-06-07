"""
Sleep Audio Processor Lambda Function

This Lambda function serves as a placeholder for future audio processing,
metadata enrichment, or validation logic. It receives input from the Step
Functions state machine, logs the input, performs basic validation, and
returns a response.

Environment Variables:
    METADATA_TABLE_NAME: DynamoDB table name for metadata storage
    OUTPUT_BUCKET_NAME: S3 bucket name for output files
"""

import json
import os
import logging
from datetime import datetime
from typing import Dict, Any

# Configure logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)

# Environment variables
METADATA_TABLE_NAME = os.environ.get('METADATA_TABLE_NAME', '')
OUTPUT_BUCKET_NAME = os.environ.get('OUTPUT_BUCKET_NAME', '')

# Supported file extensions for audio processing
SUPPORTED_EXTENSIONS = {'.mp3', '.wav', '.m4a', '.txt', '.json'}


def validate_file_extension(object_key: str) -> None:
    """
    Validate that the file has a supported extension.
    
    Args:
        object_key: S3 object key to validate
        
    Raises:
        ValueError: If file extension is not supported
    """
    # Extract file extension (convert to lowercase for case-insensitive matching)
    extension = os.path.splitext(object_key.lower())[1]
    
    if not extension:
        raise ValueError(f"File has no extension: {object_key}")
    
    if extension not in SUPPORTED_EXTENSIONS:
        supported = ', '.join(sorted(SUPPORTED_EXTENSIONS))
        raise ValueError(
            f"Unsupported file extension '{extension}'. "
            f"Supported extensions are: {supported}"
        )
    
    logger.info(f"File extension '{extension}' is valid")


def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    Lambda handler for audio processing pipeline.
    
    This function receives S3 event details from the Step Functions state machine,
    logs the input, performs input validation (bucket, key, file extension), 
    and returns enriched metadata.
    
    Input validation ensures:
    - Bucket name and object key are present
    - File extension is one of: .mp3, .wav, .m4a, .txt, .json
    
    If validation fails, the function raises an exception which will be caught
    by the Step Functions state machine and routed to the error handling path.
    
    Args:
        event: Input event from Step Functions containing S3 event details
        context: Lambda context object
        
    Returns:
        Dictionary with processing status and metadata
        
    Raises:
        ValueError: If input validation fails (missing fields or unsupported file type)
    """
    try:
        # Log the incoming event for debugging
        logger.info(f"Received event: {json.dumps(event)}")
        logger.info(f"Table: {METADATA_TABLE_NAME}, Bucket: {OUTPUT_BUCKET_NAME}")
        
        # Extract S3 event details from the input
        detail = event.get('detail', {})
        bucket_name = detail.get('bucket', {}).get('name', '')
        object_key = detail.get('object', {}).get('key', '')
        
        # Generate audio ID
        audio_id = f"s3-{bucket_name}-{object_key}" if bucket_name and object_key else "unknown"
        
        logger.info(f"Processing audio: {audio_id}")
        logger.info(f"Bucket: {bucket_name}, Key: {object_key}")
        
        # Input validation: Check required fields
        if not bucket_name or not object_key:
            raise ValueError("Missing required S3 event details: bucket name or object key")
        
        # Input validation: Check file extension (Issue #8)
        validate_file_extension(object_key)
        
        # Placeholder for future advanced processing logic:
        # - Validate file format (MP3, WAV, M4A, or TXT)
        # - Extract metadata (file size, duration, MIME type)
        # - Update DynamoDB status
        # - Perform audio analysis or validation
        
        # Return success response with enriched metadata
        return {
            'statusCode': 200,
            'status': 'success',
            'audioId': audio_id,
            'bucket': bucket_name,
            'key': object_key,
            'processedAt': datetime.utcnow().isoformat() + 'Z',
            'message': 'Audio processor executed successfully'
        }
        
    except Exception as e:
        logger.error(f"Error processing audio: {str(e)}", exc_info=True)
        # Re-raise the exception so Step Functions can catch it and route to error handling
        # This ensures the state machine Catch block is triggered and the pipeline
        # transitions to the failure path (UpdateStatusToFailed → PublishFailureNotification)
        raise
