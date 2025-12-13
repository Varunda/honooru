<template>
    <div>
        <div v-if="pools.state == 'idle'"></div>

        <div v-else-if="pools.state == 'loading'">
            loading...
            <busy></busy>
        </div>

        <div v-else-if="pools.state == 'loaded'">
            <a v-for="pool in pools.data" :key="pool.id" :href="'/pool/' + pool.id" target="_blank">
                {{ pool.name }}
            </a>
        </div>

        <div v-else-if="pools.state == 'error'">
            <api-error :error="pools.problem"></api-error>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    import { Busy } from "components/Busy.vue";
    import ApiError from "components/ApiError";

    export const PostPools = Vue.extend({
        props: {
            PostId: { type: Number, required: true }
        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<PostPool[]>
            }
        },

        mounted: function() {  
            this.$nextTick(() => {
                this.bind();
            });
        },

        methods: {
            bind: async function(): Promise<void> {
                this.pools = Loadable.loading();
                this.pools = await PostPoolApi.getByPostID(this.PostId);
            }
        },

        components: {
            Busy, ApiError
        }
    });
    export default PostPools;
</script>