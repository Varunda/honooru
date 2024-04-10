<template>
    <button v-on="$listeners" :disabled="!hasPermission"
            :title="hasPermission ? '' : 'you do not have permission to perform this action! (' + permission + ')'"
            style="[ hasPermission ? '' : 'pointer-events: auto' ]">
        <slot></slot>
    </button>
</template>

<script lang="ts">
    import Vue from "vue";

    import AccountUtil from "util/AccountUtil";

    export const PermissionedButton = Vue.extend({
        props: {
            permission: { type: String, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        computed: {
            hasPermission: function(): boolean {
                return AccountUtil.hasPermission(this.permission);
            }
        }
    });
    export default PermissionedButton;
</script>