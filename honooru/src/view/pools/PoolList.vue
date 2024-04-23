<template>
    <div>
        <app-menu></app-menu>

        <div>
            <h1>
                Pools
            </h1>

            <div v-if="pools.state == 'idle'"></div>

            <div v-else-if="pools.state == 'loading'">
                loading...
            </div>

            <div v-else-if="pools.state == 'loaded'">
                <a v-for="pool in pools.data" :href="'/pool/' + pool.id">
                    {{pool.name}}
                </a>

            </div>
        </div>


    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import AlertCollapse from "components/AlertCollapse.vue";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    export const PoolList = Vue.extend({
        props: {

        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<PostPool[]>
            }
        },

        mounted: function(): void {
            this.loadAll();
        },

        methods: {
            loadAll: async function(): Promise<void> {
                this.pools = Loadable.loading();
                this.pools = await PostPoolApi.getAll();
            }

        },

        components: {
            InfoHover, ApiError, AlertCollapse,
            AppMenu,
            PostList,
        }

    });
    export default PoolList;

</script>