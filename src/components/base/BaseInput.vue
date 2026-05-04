<template>
  <div class="base-input-group">
    <label v-if="label" :for="id" class="base-label">
      {{ label }}
      <span v-if="required" class="required-mark">*</span>
    </label>
    <div class="input-wrapper" :class="{ 'has-icon': $slots.icon, 'has-append': $slots.append }">
      <span v-if="$slots.icon" class="input-icon">
        <slot name="icon"></slot>
      </span>
      <input
        :id="id"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :required="required"
        class="base-input"
        @input="$emit('update:modelValue', $event.target.value)"
      />
      <span v-if="$slots.append" class="input-append">
        <slot name="append"></slot>
      </span>
    </div>
    <span v-if="error" class="error-text">{{ error }}</span>
  </div>
</template>

<script setup>
defineProps({
  modelValue: [String, Number],
  label: String,
  type: {
    type: String,
    default: 'text'
  },
  placeholder: String,
  id: String,
  disabled: Boolean,
  required: Boolean,
  error: String
});

defineEmits(['update:modelValue']);
</script>

<style scoped>
.base-input-group {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  width: 100%;
}

.base-label {
  font-size: 13px;
  font-weight: 600;
  color: var(--foreground);
  opacity: 0.8;
}

.required-mark {
  color: #ef4444;
  margin-left: 2px;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.base-input {
  width: 100%;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  background: var(--glass-bg);
  color: var(--foreground);
  font-size: 14px;
  transition: all 0.2s ease;
}

.base-input:focus {
  outline: none;
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(var(--primary-h), var(--primary-s), var(--primary-l), 0.1);
  background: white;
}

.has-icon .base-input {
  padding-left: 36px;
}

.has-append .base-input {
  padding-right: 36px;
}

.input-icon {
  position: absolute;
  left: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--foreground);
  opacity: 0.5;
}

.input-append {
  position: absolute;
  right: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--foreground);
  opacity: 0.5;
}

.error-text {
  font-size: 12px;
  color: #ef4444;
  margin-top: 2px;
}

@media (prefers-color-scheme: dark) {
  .base-input:focus {
    background: rgba(255, 255, 255, 0.05);
  }
}
</style>
