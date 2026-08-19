'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Form } from 'antd';
import { globalToastId } from '@/constants/config';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphModal from '@/components/base/modal/Modal';

import LitegraphInput from '@/components/base/input/Input';
import { toast } from 'react-hot-toast';
import { BackupMetaData, BackupMetaDataCreateRequest } from 'litegraphdb/dist/types/types';
import { useCreateBackupMutation } from '@/lib/store/slice/slice';

interface AddEditBackupProps {
  isAddEditBackupVisible: boolean;
  setIsAddEditBackupVisible: (visible: boolean) => void;
  backup: BackupMetaData | null;
  onBackupUpdated?: () => Promise<void>;
}

const AddEditBackup = ({
  isAddEditBackupVisible,
  setIsAddEditBackupVisible,
  backup,
  onBackupUpdated,
}: AddEditBackupProps) => {
  const t = useTranslations('backups');
  const [form] = Form.useForm<BackupMetaDataCreateRequest>();
  const [formValid, setFormValid] = useState(false);
  const [createBackupService, { isLoading: createBackupLoading }] = useCreateBackupMutation();
  const [formValues, setFormValues] = useState({});

  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const backupData: BackupMetaDataCreateRequest = {
        Filename: values.Filename,
      };
      const success = await createBackupService(backupData);

      if (success) {
        setIsAddEditBackupVisible(false);
        form.resetFields();
        onBackupUpdated && onBackupUpdated();
        toast.success(t('toast.created'), { id: globalToastId });
      } else {
        toast.error(t('toast.createFailed'), { id: globalToastId });
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(t('toast.createFailedError', { error: errorMessage }), { id: globalToastId });
    }
  };

  return (
    <LitegraphModal
      title={backup ? t('modalTitle.edit') : t('modalTitle.create')}
      open={isAddEditBackupVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditBackupVisible(false);
        form.resetFields();
      }}
      confirmLoading={createBackupLoading}
      okButtonProps={{ disabled: !formValid }}
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label={t('form.filename')}
          name="Filename"
          tooltip={t('form.filenameTooltip')}
          rules={[{ required: true, message: t('form.filenameRequired') }]}
        >
          <LitegraphInput placeholder={t('form.filenamePlaceholder')} data-testid="filename-input" />
        </LitegraphFormItem>
      </Form>
    </LitegraphModal>
  );
};

export default AddEditBackup;
