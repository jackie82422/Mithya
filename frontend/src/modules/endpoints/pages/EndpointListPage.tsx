import { useState, useRef, useEffect, useCallback } from 'react';
import { Typography, Button, Spin, Empty, Input, Flex, Space, Popconfirm, message } from 'antd';
import type { InputRef } from 'antd';
import { PlusOutlined, SearchOutlined, CheckSquareOutlined, CloseOutlined } from '@ant-design/icons';
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

  const [batchMode, setBatchMode] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [batchLoading, setBatchLoading] = useState(false);

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
      ep.serviceName.toLowerCase().includes(search.toLowerCase()) ||
      ep.httpMethod.toLowerCase().includes(search.toLowerCase()),
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

  const handleSelectToggle = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const exitBatchMode = () => {
    setBatchMode(false);
    setSelectedIds(new Set());
  };

  const runBatch = async (action: (id: string) => Promise<unknown>) => {
    setBatchLoading(true);
    const ids = Array.from(selectedIds);
    let successCount = 0;
    for (const id of ids) {
      try {
        await action(id);
        successCount++;
      } catch {
        // individual errors handled by mutation
      }
    }
    setBatchLoading(false);
    if (successCount > 0) {
      message.success(t('endpoints.batch.done', { count: successCount }));
    }
    exitBatchMode();
  };

  const handleBatchEnable = () => {
    const toEnable = Array.from(selectedIds).filter((id) => {
      const ep = endpoints?.find((e) => e.id === id);
      return ep && !ep.isActive;
    });
    if (toEnable.length === 0) {
      message.info(t('endpoints.batch.noneToEnable'));
      return;
    }
    setSelectedIds(new Set(toEnable));
    runBatch((id) => toggleEndpoint.mutateAsync(id));
  };

  const handleBatchDisable = () => {
    const toDisable = Array.from(selectedIds).filter((id) => {
      const ep = endpoints?.find((e) => e.id === id);
      return ep && ep.isActive;
    });
    if (toDisable.length === 0) {
      message.info(t('endpoints.batch.noneToDisable'));
      return;
    }
    setSelectedIds(new Set(toDisable));
    runBatch((id) => toggleEndpoint.mutateAsync(id));
  };

  const handleBatchDelete = () => {
    runBatch((id) => deleteEndpoint.mutateAsync(id));
  };

  const selectedCount = selectedIds.size;

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
        <Space>
          {!batchMode && filtered && filtered.length > 0 && (
            <Button
              icon={<CheckSquareOutlined />}
              onClick={() => setBatchMode(true)}
            >
              {t('endpoints.batch.toggle')}
            </Button>
          )}
          {batchMode && (
            <Button icon={<CloseOutlined />} onClick={exitBatchMode}>
              {t('common.cancel')}
            </Button>
          )}
          <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingEndpoint(null); setFormOpen(true); }}>
            {t('endpoints.create')}
          </Button>
        </Space>
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
            selectable={batchMode}
            selected={selectedIds.has(ep.id)}
            onSelect={handleSelectToggle}
          />
        ))
      )}

      {batchMode && selectedCount > 0 && (
        <div
          style={{
            position: 'fixed',
            bottom: 24,
            left: '50%',
            transform: 'translateX(-50%)',
            background: 'var(--color-surface)',
            border: '1px solid var(--color-border)',
            borderRadius: 16,
            padding: '12px 24px',
            boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
            zIndex: 100,
            backdropFilter: 'blur(12px)',
          }}
        >
          <Flex align="center" gap={16}>
            <Typography.Text strong>
              {t('endpoints.batch.selected', { count: selectedCount })}
            </Typography.Text>
            <Button size="small" onClick={handleBatchEnable} loading={batchLoading}>
              {t('endpoints.batch.enable')}
            </Button>
            <Button size="small" onClick={handleBatchDisable} loading={batchLoading}>
              {t('endpoints.batch.disable')}
            </Button>
            <Popconfirm
              title={t('endpoints.batch.deleteConfirm', { count: selectedCount })}
              onConfirm={handleBatchDelete}
              okText={t('common.yes')}
              cancelText={t('common.no')}
            >
              <Button size="small" danger loading={batchLoading}>
                {t('common.delete')}
              </Button>
            </Popconfirm>
          </Flex>
        </div>
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
