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
                <pool-list-entry v-for="pool in pools.data" :key="pool.id" :pool="pool" class="mb-3 pb-3 border-bottom"></pool-list-entry>
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
    import UserAccount from "components/app/UserAccount.vue";
    import PoolListEntry from "./PoolListEntry.vue";

    import "filters/MomentFilter";

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
            document.title = "Honooru / Pools";
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
            UserAccount,
            PoolListEntry
        }

    });
    export default PoolList;

</script>