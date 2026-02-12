import { useState } from 'react';
import { Typography, Button, Flex, Empty } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { CreateServiceProxyRequest, ServiceProxy } from '@/shared/types';
import {
  useServiceProxies,
  useCreateServiceProxy,
  useUpdateServiceProxy,
  useDeleteServiceProxy,
  useToggleServiceProxy,
  useToggleRecording,
  useToggleFallback,
} from '../hooks';
import ServiceProxyCard from '../components/ServiceProxyCard';
import ServiceProxyForm from '../components/ServiceProxyForm';

export default function ProxyConfigPage() {
  const { t } = useTranslation();
  const { data: proxies, isLoading } = useServiceProxies();
  const createProxy = useCreateServiceProxy();
  const updateProxy = useUpdateServiceProxy();
  const deleteProxy = useDeleteServiceProxy();
  const toggleProxy = useToggleServiceProxy();
  const toggleRecording = useToggleRecording();
  const toggleFallback = useToggleFallback();
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<ServiceProxy | null>(null);

  const handleSubmit = (values: CreateServiceProxyRequest) => {
    if (editing) {
      updateProxy.mutate(
        { id: editing.id, data: values },
        { onSuccess: () => { setFormOpen(false); setEditing(null); } },
      );
    } else {
      createProxy.mutate(values, {
        onSuccess: () => setFormOpen(false),
      });
    }
  };

  const handleEdit = (proxy: ServiceProxy) => {
    setEditing(proxy);
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
        {!isLoading && (!proxies || proxies.length === 0) ? (
          <Empty description={t('proxy.noConfigs')}>
            <Button type="primary" onClick={() => setFormOpen(true)}>
              {t('common.create')}
            </Button>
          </Empty>
        ) : (
          proxies?.map((proxy) => (
            <ServiceProxyCard
              key={proxy.id}
              proxy={proxy}
              onEdit={handleEdit}
              onDelete={(id) => deleteProxy.mutate(id)}
              onToggle={(id) => toggleProxy.mutate(id)}
              onToggleRecording={(id) => toggleRecording.mutate(id)}
              onToggleFallback={(id) => toggleFallback.mutate(id)}
            />
          ))
        )}
      </div>

      <ServiceProxyForm
        open={formOpen}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        loading={editing ? updateProxy.isPending : createProxy.isPending}
        editing={editing}
      />
    </div>
  );
}
