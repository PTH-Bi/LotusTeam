<template>
  <div class="base-table-container glass-card">
    <table class="base-table">
      <thead>
        <tr>
          <th v-for="col in columns" :key="col.key" :style="{ width: col.width }">
            {{ col.label }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(row, index) in data" :key="index">
          <td v-for="col in columns" :key="col.key">
            <slot :name="col.key" :row="row" :index="index">
              {{ row[col.key] }}
            </slot>
          </td>
        </tr>
        <tr v-if="data.length === 0">
          <td :colspan="columns.length" class="empty-state">
            Không có dữ liệu hiển thị
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
defineProps({
  columns: {
    type: Array,
    required: true
    /**
     * col: { key: string, label: string, width?: string }
     */
  },
  data: {
    type: Array,
    required: true
  }
});
</script>

<style scoped>
.base-table-container {
  width: 100%;
  overflow-x: auto;
  border-radius: var(--radius-lg);
}

.base-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

th {
  padding: var(--space-4);
  background: rgba(0, 0, 0, 0.02);
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--foreground);
  opacity: 0.7;
  border-bottom: 1px solid var(--glass-border);
}

td {
  padding: var(--space-4);
  font-size: 14px;
  border-bottom: 1px solid var(--glass-border);
  color: var(--foreground);
}

tr:last-child td {
  border-bottom: none;
}

tr:hover td {
  background: rgba(var(--primary-h), var(--primary-s), var(--primary-l), 0.02);
}

.empty-state {
  text-align: center;
  padding: var(--space-8);
  opacity: 0.5;
  font-style: italic;
}

@media (prefers-color-scheme: dark) {
  th {
    background: rgba(255, 255, 255, 0.05);
  }
}
</style>
