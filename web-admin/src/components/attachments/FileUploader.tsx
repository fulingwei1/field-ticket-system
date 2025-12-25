import React, { useState } from 'react';
import { Upload, Button, message, Progress, List, Space, Typography } from 'antd';
import { UploadOutlined, DeleteOutlined, EyeOutlined } from '@ant-design/icons';
import type { UploadFile, UploadProps } from 'antd/es/upload';
import { attachmentService, AttachmentDto } from '../../services/attachmentService';

const { Text } = Typography;

interface FileUploaderProps {
  ticketId: string;
  fileType: 'photo' | 'video' | 'log' | 'file';
  onUploadSuccess?: (attachment: AttachmentDto) => void;
  onDelete?: (attachmentId: string) => void;
  maxCount?: number;
  maxSize?: number; // MB
}

export const FileUploader: React.FC<FileUploaderProps> = ({
  ticketId,
  fileType,
  onUploadSuccess,
  onDelete,
  maxCount = 10,
  maxSize,
}) => {
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});

  const maxSizeBytes = maxSize ? maxSize * 1024 * 1024 : undefined;

  const handleUpload: UploadProps['customRequest'] = async (options) => {
    const { file, onSuccess, onError } = options;
    const fileObj = file as File;

    // 验证文件大小
    if (maxSizeBytes && fileObj.size > maxSizeBytes) {
      const error = new Error(`文件大小超过限制（最大 ${maxSize}MB）`);
      onError?.(error);
      message.error(error.message);
      return;
    }

    setUploading(true);
    const fileId = `${Date.now()}_${fileObj.name}`;
    setUploadProgress((prev) => ({ ...prev, [fileId]: 0 }));

    try {
      // 模拟上传进度（实际应该使用 XMLHttpRequest 来获取真实进度）
      const progressInterval = setInterval(() => {
        setUploadProgress((prev) => {
          const current = prev[fileId] || 0;
          if (current < 90) {
            return { ...prev, [fileId]: current + 10 };
          }
          return prev;
        });
      }, 200);

      const attachment = await attachmentService.uploadAttachment(ticketId, fileObj, fileType);

      clearInterval(progressInterval);
      setUploadProgress((prev) => ({ ...prev, [fileId]: 100 }));

      message.success(`${fileObj.name} 上传成功`);
      onSuccess?.(attachment, new XMLHttpRequest());

      if (onUploadSuccess) {
        onUploadSuccess(attachment);
      }

      // 更新文件列表
      setFileList((prev) => {
        const newList = [...prev];
        const index = newList.findIndex((item) => item.uid === fileId);
        if (index >= 0) {
          newList[index] = {
            ...newList[index],
            status: 'done',
            response: attachment,
          };
        }
        return newList;
      });
    } catch (error: any) {
      message.error(error.message || '上传失败');
      onError?.(error);

      // 更新文件列表
      setFileList((prev) => {
        const newList = [...prev];
        const index = newList.findIndex((item) => item.uid === fileId);
        if (index >= 0) {
          newList[index] = {
            ...newList[index],
            status: 'error',
          };
        }
        return newList;
      });
    } finally {
      setUploading(false);
      setTimeout(() => {
        setUploadProgress((prev) => {
          const newPrev = { ...prev };
          delete newPrev[fileId];
          return newPrev;
        });
      }, 1000);
    }
  };

  const handleRemove = async (file: UploadFile) => {
    if (file.response && (file.response as AttachmentDto).attachmentId) {
      const attachmentId = (file.response as AttachmentDto).attachmentId;
      try {
        await attachmentService.deleteAttachment(attachmentId);
        message.success('附件删除成功');
        if (onDelete) {
          onDelete(attachmentId);
        }
      } catch (error: any) {
        message.error(error.message || '删除失败');
      }
    }
  };

  const handlePreview = async (file: UploadFile) => {
    if (file.response && (file.response as AttachmentDto).attachmentId) {
      const attachmentId = (file.response as AttachmentDto).attachmentId;
      try {
        const url = await attachmentService.getDownloadUrl(attachmentId);
        window.open(url, '_blank');
      } catch (error: any) {
        message.error(error.message || '获取预览链接失败');
      }
    }
  };

  const getAccept = () => {
    switch (fileType) {
      case 'photo':
        return 'image/*';
      case 'video':
        return 'video/*';
      case 'log':
        return '.txt,.log,.json,.csv';
      default:
        return '*';
    }
  };

  return (
    <div>
      <Upload
        fileList={fileList}
        customRequest={handleUpload}
        onRemove={handleRemove}
        onPreview={handlePreview}
        maxCount={maxCount}
        accept={getAccept()}
        disabled={uploading}
      >
        <Button icon={<UploadOutlined />} loading={uploading} disabled={uploading}>
          上传文件
        </Button>
      </Upload>

      {/* 显示上传进度 */}
      {Object.keys(uploadProgress).length > 0 && (
        <div style={{ marginTop: 16 }}>
          {Object.entries(uploadProgress).map(([fileId, progress]) => {
            const file = fileList.find((f) => f.uid === fileId);
            return (
              <div key={fileId} style={{ marginBottom: 8 }}>
                <Text type="secondary">{file?.name}</Text>
                <Progress percent={progress} size="small" />
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

