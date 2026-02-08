import { useState, useRef, useEffect } from 'react';
import { Typography, Button, Spin, Empty, Input, Flex } from 'antd';
import type { InputRef } from 'antd';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoints, useCreateEndpoint, useUpdateEndpoint, useDeleteEndpoint, useSetDefaultResponse, useToggleEndpoint } from '../hooks';
import EndpointCard from '../components/EndpointCard';
import EndpointForm from '../components/EndpointForm';
import DefaultResponseForm from '../components/DefaultResponseForm';
import type { MockEndpoint, CreateEndpointRequest, UpdateEndpointRequest, SetDefaultResponseRequest } from '@/shared/types';

export default function EndpointListPage() {
  const { t } = useTranslation();
  const { data: endpoints, isLoading } = useEndpoints();
  const createEndpoint = useCreateEndpoint();
  const updateEndpoint = useUpdateEndpoint();
  const deleteEndpoint = useDeleteEndpoint();
  const setDefault = useSetDefaultResponse();
  const toggleEndpoint = useToggleEndpoint();

  const [formOpen, setFormOpen] = useState(false);
  const [editingEndpoint, setEditingEndpoint] = useState<MockEndpoint | null>(null);
  const [defaultFormOpen, setDefaultFormOpen] = useState(false);
  const [selectedEndpoint, setSelectedEndpoint] = useState<MockEndpoint | null>(null);
  const [search, setSearch] = useState('');
  const searchRef = useRef<InputRef>(null);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, []);

  const filtered = endpoints?.filter(
    (ep) =>
      ep.name.toLowerCase().includes(search.toLowerCase()) ||
      ep.path.toLowerCase().includes(search.toLowerCase()) ||
      ep.serviceName.toLowerCase().includes(search.toLowerCase()),
  );

  const handleCreate = (values: CreateEndpointRequest) => {
    if (editingEndpoint) {
      const updateData: UpdateEndpointRequest = {
        name: values.name,
        serviceName: values.serviceName,
        path: values.path,
        httpMethod: values.httpMethod,
        protocolSettings: values.protocolSettings,
      };
      updateEndpoint.mutate(
        { id: editingEndpoint.id, data: updateData },
        { onSuccess: () => { setFormOpen(false); setEditingEndpoint(null); } },
      );
    } else {
      createEndpoint.mutate(values, { onSuccess: () => setFormOpen(false) });
    }
  };

  const handleSetDefault = (id: string, data: SetDefaultResponseRequest) => {
    setDefault.mutate({ id, data }, { onSuccess: () => setDefaultFormOpen(false) });
  };

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 24 }}>
        <div>
          <Typography.Title level={3} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {t('endpoints.title')}
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 14 }}>
            {t('endpoints.subtitle')}
          </Typography.Text>
        </div>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingEndpoint(null); setFormOpen(true); }}>
          {t('endpoints.create')}
        </Button>
      </Flex>

      <Input
        ref={searchRef}
        prefix={<SearchOutlined style={{ color: 'var(--color-text-secondary)' }} />}
        placeholder={`${t('common.search')}  ⌘K`}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        style={{ marginBottom: 20, height: 42, borderRadius: 12 }}
        allowClear
      />

      {isLoading ? (
        <Flex justify="center" style={{ padding: 80 }}>
          <Spin size="large" />
        </Flex>
      ) : !filtered?.length ? (
        <Empty description={search ? t('common.noData') : t('endpoints.noEndpoints')}>
          {!search && (
            <Button type="primary" onClick={() => setFormOpen(true)}>
              {t('endpoints.create')}
            </Button>
          )}
        </Empty>
      ) : (
        filtered.map((ep) => (
          <EndpointCard
            key={ep.id}
            endpoint={ep}
            onDelete={(id) => deleteEndpoint.mutate(id)}
            onSetDefault={(ep) => {
              setSelectedEndpoint(ep);
              setDefaultFormOpen(true);
            }}
            onToggle={(id) => toggleEndpoint.mutate(id)}
            onEdit={(ep) => {
              setEditingEndpoint(ep);
              setFormOpen(true);
            }}
            toggleLoading={toggleEndpoint.isPending}
          />
        ))
      )}

      <EndpointForm
        open={formOpen}
        onCancel={() => { setFormOpen(false); setEditingEndpoint(null); }}
        onSubmit={handleCreate}
        loading={editingEndpoint ? updateEndpoint.isPending : createEndpoint.isPending}
        editingEndpoint={editingEndpoint}
      />

      <DefaultResponseForm
        open={defaultFormOpen}
        endpoint={selectedEndpoint}
        onCancel={() => setDefaultFormOpen(false)}
        onSubmit={handleSetDefault}
        loading={setDefault.isPending}
      />
    </div>
  );
}
