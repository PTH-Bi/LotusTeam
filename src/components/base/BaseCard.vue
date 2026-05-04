<template>
  <div 
    :class="[
      'base-card', 
      { 'glass-effect': glass, 'hover-effect': hover }
    ]"
  >
    <div v-if="$slots.header" class="card-header">
      <slot name="header"></slot>
    </div>
    <div class="card-body">
      <slot></slot>
    </div>
    <div v-if="$slots.footer" class="card-footer">
      <slot name="footer"></slot>
    </div>
  </div>
</template>

<script setup>
defineProps({
  glass: {
    type: Boolean,
    default: true
  },
  hover: {
    type: Boolean,
    default: false
  }
});
</script>

<style scoped>
.base-card {
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.glass-effect {
  background: var(--glass-bg);
  backdrop-filter: var(--glass-blur);
  -webkit-backdrop-filter: var(--glass-blur);
  border: 1px solid var(--glass-border);
}

.hover-effect:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.05);
}

.card-header {
  padding: var(--space-4);
  border-bottom: 1px solid var(--glass-border);
  font-weight: 600;
}

.card-body {
  padding: var(--space-4);
}

.card-footer {
  padding: var(--space-4);
  border-top: 1px solid var(--glass-border);
  background: rgba(0, 0, 0, 0.02);
}
</style>
