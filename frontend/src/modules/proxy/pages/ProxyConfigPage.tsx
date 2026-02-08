import { useState } from 'react';
import { Typography, Button, Flex, Empty } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { CreateProxyConfigRequest, ProxyConfig } from '@/shared/types';
import { useEndpoints } from '@/modules/endpoints/hooks';
import {
  useProxyConfigs,
  useCreateProxyConfig,
  useUpdateProxyConfig,
  useDeleteProxyConfig,
  useToggleProxyConfig,
  useToggleRecording,
} from '../hooks';
import ProxyConfigCard from '../components/ProxyConfigCard';
import ProxyConfigForm from '../components/ProxyConfigForm';

export default function ProxyConfigPage() {
  const { t } = useTranslation();
  const { data: configs, isLoading } = useProxyConfigs();
  const { data: endpoints } = useEndpoints();
  const createConfig = useCreateProxyConfig();
  const updateConfig = useUpdateProxyConfig();
  const deleteConfig = useDeleteProxyConfig();
  const toggleConfig = useToggleProxyConfig();
  const toggleRecording = useToggleRecording();
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<ProxyConfig | null>(null);

  const handleSubmit = (values: CreateProxyConfigRequest) => {
    if (editing) {
      updateConfig.mutate(
        { id: editing.id, data: values },
        { onSuccess: () => { setFormOpen(false); setEditing(null); } },
      );
    } else {
      createConfig.mutate(values, {
        onSuccess: () => setFormOpen(false),
      });
    }
  };

  const handleEdit = (config: ProxyConfig) => {
    setEditing(config);
    setFormOpen(true);
  };

  const handleCancel = () => {
    setFormOpen(false);
    setEditing(null);
  };

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
        <div>
          <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {t('proxy.title')}
          </Typography.Title>
          <Typography.Text type="secondary">{t('proxy.subtitle')}</Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditing(null);
            setFormOpen(true);
          }}
        >
          {t('common.create')}
        </Button>
      </Flex>

      <div style={{ marginTop: 24 }}>
        {!isLoading && (!configs || configs.length === 0) ? (
          <Empty description={t('proxy.noConfigs')}>
            <Button type="primary" onClick={() => setFormOpen(true)}>
              {t('common.create')}
            </Button>
          </Empty>
        ) : (
          configs?.map((config) => (
            <ProxyConfigCard
              key={config.id}
              config={config}
              endpoints={endpoints}
              onEdit={handleEdit}
              onDelete={(id) => deleteConfig.mutate(id)}
              onToggle={(id) => toggleConfig.mutate(id)}
              onToggleRecording={(id) => toggleRecording.mutate(id)}
              toggleLoading={toggleConfig.isPending}
            />
          ))
        )}
      </div>

      <ProxyConfigForm
        open={formOpen}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        loading={editing ? updateConfig.isPending : createConfig.isPending}
        editing={editing}
        endpoints={endpoints}
      />
    </div>
  );
}
