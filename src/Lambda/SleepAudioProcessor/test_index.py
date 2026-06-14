"""
Unit tests for Sleep Audio Processor Lambda Function

Tests the Lambda handler logic, input validation, and error handling.
Uses mocking to avoid AWS service dependencies.

Run tests with: pytest test_index.py -v
"""

import pytest
import json
import os
from unittest.mock import MagicMock, patch
from datetime import datetime

# Import the Lambda handler and validation function
import index
from index import lambda_handler, validate_file_extension


class TestValidateFileExtension:
    """Test suite for file extension validation."""
    
    def test_valid_mp3_extension(self):
        """Test that .mp3 extension is valid."""
        # Should not raise exception
        validate_file_extension("test-audio.mp3")
    
    def test_valid_wav_extension(self):
        """Test that .wav extension is valid."""
        validate_file_extension("test-audio.wav")
    
    def test_valid_m4a_extension(self):
        """Test that .m4a extension is valid."""
        validate_file_extension("test-audio.m4a")
    
    def test_valid_txt_extension(self):
        """Test that .txt extension is valid."""
        validate_file_extension("test-text.txt")
    
    def test_valid_json_extension(self):
        """Test that .json extension is valid."""
        validate_file_extension("test-config.json")
    
    def test_case_insensitive_validation(self):
        """Test that validation is case-insensitive."""
        validate_file_extension("test-audio.MP3")
        validate_file_extension("test-audio.WaV")
    
    def test_invalid_extension_raises_error(self):
        """Test that invalid extension raises ValueError."""
        with pytest.raises(ValueError) as exc_info:
            validate_file_extension("test-audio.xyz")
        assert "Unsupported file extension" in str(exc_info.value)
    
    def test_no_extension_raises_error(self):
        """Test that file without extension raises ValueError."""
        with pytest.raises(ValueError) as exc_info:
            validate_file_extension("test-audio")
        assert "File has no extension" in str(exc_info.value)


class TestLambdaHandler:
    """Test suite for Lambda handler function."""
    
    @pytest.fixture
    def mock_context(self):
        """Create a mock Lambda context object."""
        context = MagicMock()
        context.request_id = "test-request-id-12345"
        context.function_name = "SleepAudioProcessorFunction"
        return context
    
    @pytest.fixture
    def valid_event(self):
        """Create a valid S3 event for testing."""
        return {
            'detail': {
                'bucket': {
                    'name': 'test-input-bucket'
                },
                'object': {
                    'key': 'audio/test-file.mp3'
                }
            }
        }
    
    @pytest.fixture(autouse=True)
    def setup_environment(self):
        """Set up environment variables for tests."""
        os.environ['METADATA_TABLE_NAME'] = 'test-metadata-table'
        os.environ['OUTPUT_BUCKET_NAME'] = 'test-output-bucket'
        yield
        # Cleanup
        if 'METADATA_TABLE_NAME' in os.environ:
            del os.environ['METADATA_TABLE_NAME']
        if 'OUTPUT_BUCKET_NAME' in os.environ:
            del os.environ['OUTPUT_BUCKET_NAME']
    
    def test_successful_processing(self, valid_event, mock_context):
        """Test successful audio file processing."""
        result = lambda_handler(valid_event, mock_context)
        
        # Verify response structure
        assert result['statusCode'] == 200
        assert result['status'] == 'success'
        assert result['bucket'] == 'test-input-bucket'
        assert result['key'] == 'audio/test-file.mp3'
        assert 'audioId' in result
        assert 'processedAt' in result
        assert result['message'] == 'Audio processor executed successfully'
    
    def test_audio_id_generation(self, valid_event, mock_context):
        """Test that audio ID is generated correctly."""
        result = lambda_handler(valid_event, mock_context)
        
        expected_audio_id = "s3-test-input-bucket-audio/test-file.mp3"
        assert result['audioId'] == expected_audio_id
    
    def test_missing_bucket_name_raises_error(self, mock_context):
        """Test that missing bucket name raises ValueError."""
        event = {
            'detail': {
                'bucket': {
                    'name': ''
                },
                'object': {
                    'key': 'test-file.mp3'
                }
            }
        }
        
        with pytest.raises(ValueError) as exc_info:
            lambda_handler(event, mock_context)
        assert "Missing required S3 event details" in str(exc_info.value)
    
    def test_missing_object_key_raises_error(self, mock_context):
        """Test that missing object key raises ValueError."""
        event = {
            'detail': {
                'bucket': {
                    'name': 'test-bucket'
                },
                'object': {
                    'key': ''
                }
            }
        }
        
        with pytest.raises(ValueError) as exc_info:
            lambda_handler(event, mock_context)
        assert "Missing required S3 event details" in str(exc_info.value)
    
    def test_invalid_file_extension_raises_error(self, mock_context):
        """Test that invalid file extension raises ValueError."""
        event = {
            'detail': {
                'bucket': {
                    'name': 'test-bucket'
                },
                'object': {
                    'key': 'test-file.xyz'
                }
            }
        }
        
        with pytest.raises(ValueError) as exc_info:
            lambda_handler(event, mock_context)
        assert "Unsupported file extension" in str(exc_info.value)
    
    def test_processing_txt_file(self, mock_context):
        """Test processing text file (for TTS)."""
        event = {
            'detail': {
                'bucket': {
                    'name': 'test-bucket'
                },
                'object': {
                    'key': 'text/sleep-story.txt'
                }
            }
        }
        
        result = lambda_handler(event, mock_context)
        assert result['statusCode'] == 200
        assert result['key'] == 'text/sleep-story.txt'
    
    def test_processing_json_file(self, mock_context):
        """Test processing JSON configuration file."""
        event = {
            'detail': {
                'bucket': {
                    'name': 'test-bucket'
                },
                'object': {
                    'key': 'config/settings.json'
                }
            }
        }
        
        result = lambda_handler(event, mock_context)
        assert result['statusCode'] == 200
        assert result['key'] == 'config/settings.json'
    
    def test_context_request_id_logging(self, valid_event, mock_context):
        """Test that request ID from context is used in processing."""
        # This is mainly a smoke test to ensure context is accessed correctly
        result = lambda_handler(valid_event, mock_context)
        assert result['statusCode'] == 200
    
    def test_none_context_handling(self, valid_event):
        """Test that None context is handled gracefully."""
        # Lambda should still work even if context is None (edge case)
        result = lambda_handler(valid_event, None)
        assert result['statusCode'] == 200
