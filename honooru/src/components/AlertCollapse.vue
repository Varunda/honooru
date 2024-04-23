<template>
    <div class="border-top mt-2 pt-2 alert alert-light p-mb-0">
        <h4 class="border-bottom pb-1 cursor-pointer" data-bs-toggle="collapse" :data-bs-target="'#' + elementID">
            <span :id="'icon-' + elementID" :class="[ opened == true ? 'bi-chevron-down' : 'bi-chevron-right' ]"></span>
            <slot name="header"></slot>
        </h4>

        <div :id="elementID" class="collapse show">
            <slot name="body"></slot>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";

    export const AlertCollapse = Vue.extend({
        props: {
            show: { type: Boolean, required: false, default: true },
        },

        data: function() {
            return {
                id: Math.floor(Math.random() * 1000000),

                opened: this.show as boolean,

                icon: null as HTMLElement | null,
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.icon = document.getElementById(`icon-${this.elementID}`);

                this.addListeners();
            });
        },

        methods: {
            addListeners: function(): void {
                document.getElementById(this.elementID)?.addEventListener("show.bs.collapse", () => {
                    this.opened = true;
                    //console.log(`showing`);
                });

                document.getElementById(this.elementID)?.addEventListener("hide.bs.collapse", () => {
                    this.opened = false;
                    //console.log(`hiding`);
                });
            },
        },

        computed: {
            elementID: function(): string {
                return `alert-collapse-${this.id}`;
            }
        }
    });
    export default AlertCollapse;
</script>
