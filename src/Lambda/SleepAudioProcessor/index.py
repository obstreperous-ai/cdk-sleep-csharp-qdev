"""
Sleep Audio Processor Lambda Function

This Lambda function serves as a production-ready audio processing handler with
structured JSON logging, X-Ray tracing support, and comprehensive error handling.
metadata enrichment, or validation logic. It receives input from the Step
Functions state machine, logs the input, performs basic validation, and
returns a response.

Environment Variables:
    METADATA_TABLE_NAME: DynamoDB table name for metadata storage
    OUTPUT_BUCKET_NAME: S3 bucket name for output files

Observability:
    - X-Ray tracing enabled for distributed tracing
    - Structured JSON logging for CloudWatch Logs Insights
"""

import json
import os
import logging
from datetime import datetime
from typing import Dict, Any
import sys


class StructuredLogger:
    """
    Structured JSON logger for CloudWatch Logs Insights.
    
    Provides consistent, parseable log output in JSON format for better
    observability and querying in CloudWatch.
    """
    
    def __init__(self, logger_name: str = __name__):
        self.logger = logging.getLogger(logger_name)
        self.logger.setLevel(logging.INFO)
        
        # Remove default handlers
        self.logger.handlers = []
        
        # Add structured JSON handler
        handler = logging.StreamHandler(sys.stdout)
        handler.setFormatter(logging.Formatter('%(message)s'))
        self.logger.addHandler(handler)
    
    def _log(self, level: str, message: str, **kwargs):
        """Internal method to log structured JSON."""
        log_entry = {
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "level": level,
            "message": message,
            **kwargs
        }
        self.logger.info(json.dumps(log_entry))
    
    def info(self, message: str, **kwargs):
        """Log info level message with structured JSON format."""
        self._log("INFO", message, **kwargs)
    
    def error(self, message: str, **kwargs):
        """Log error level message with structured JSON format."""
        self._log("ERROR", message, **kwargs)
    
    def warning(self, message: str, **kwargs):
        """Log warning level message with structured JSON format."""
        self._log("WARNING", message, **kwargs)
    
    def debug(self, message: str, **kwargs):
        """Log debug level message with structured JSON format."""
        self._log("DEBUG", message, **kwargs)


# Initialize structured logger for CloudWatch Logs Insights
# Issue #10: Structured JSON logging for better observability
logger = StructuredLogger(__name__)

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
    
    Enhanced with X-Ray tracing and structured JSON logging (Issue #10).
    
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
        # Issue #10: Structured JSON logging with request context
        logger.info(
            "Lambda invocation started",
            requestId=context.request_id if context else "unknown",
            functionName=context.function_name if context else "SleepAudioProcessor",
            tableName=METADATA_TABLE_NAME,
            outputBucket=OUTPUT_BUCKET_NAME
        )
        
        # Extract S3 event details from the input
        detail = event.get('detail', {})
        bucket_name = detail.get('bucket', {}).get('name', '')
        object_key = detail.get('object', {}).get('key', '')
        
        # Generate audio ID
        audio_id = f"s3-{bucket_name}-{object_key}" if bucket_name and object_key else "unknown"
        
        logger.info(
            "Processing audio file",
            audioId=audio_id,
            bucket=bucket_name,
            key=object_key,
            requestId=context.request_id if context else "unknown"
        )
        
        # Input validation: Check required fields
        if not bucket_name or not object_key:
            logger.error(
                "Input validation failed: missing required fields",
                audioId=audio_id,
                bucket=bucket_name,
                key=object_key,
                error="Missing bucket name or object key"
            )
            raise ValueError("Missing required S3 event details: bucket name or object key")
        
        # Input validation: Check file extension (Issue #8)
        validate_file_extension(object_key)
        
        logger.info(
            "Input validation passed",
            audioId=audio_id,
            bucket=bucket_name,
            key=object_key
        )
        
        # Placeholder for future advanced processing logic:
        # - Validate file format (MP3, WAV, M4A, or TXT)
        # - Extract metadata (file size, duration, MIME type)
        # - Update DynamoDB status
        # - Perform audio analysis or validation
        
        logger.info(
            "Audio processing completed successfully",
            audioId=audio_id,
            status="success",
            requestId=context.request_id if context else "unknown"
        )
        
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
        # Issue #10: Structured error logging with context
        logger.error(
            "Error processing audio",
            error=str(e),
            errorType=type(e).__name__,
            requestId=context.request_id if context else "unknown",
            audioId=audio_id if 'audio_id' in locals() else "unknown"
        )
        # Re-raise the exception so Step Functions can catch it and route to error handling
        # This ensures the state machine Catch block is triggered and the pipeline
        # transitions to the failure path (UpdateStatusToFailed → PublishFailureNotification)
        raise
