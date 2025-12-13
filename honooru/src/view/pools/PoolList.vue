<template>
    <div>
        <app-menu></app-menu>

        <div>
            <h1 class="d-flex">
                <div class="flex-grow-1">
                    Pools
                </div>

                <div v-if="hasCreatePoolPermission" class="flex-grow-0 input-group" style="max-width: 400px;">
                    <input v-model="newPool" class="form-control d-inline" type="text" placeholder="Pool name"/>
                    <button class="btn btn-primary d-inline" @click="createPool">
                        create
                    </button>
                </div>
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

    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import AlertCollapse from "components/AlertCollapse.vue";
    import PostList from "components/app/PostList.vue";
    import UserAccount from "components/app/UserAccount.vue";

    import PoolListEntry from "./PoolListEntry.vue";

    import "filters/MomentFilter";

    import AccountUtil from "util/AccountUtil";
    import Toaster from "Toaster";

    export const PoolList = Vue.extend({
        props: {

        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<PostPool[]>,

                newPool: "" as string
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
            },

            createPool: async function(): Promise<Loading<PostPool>> {
                const ret: Loading<PostPool> = await PostPoolApi.create(this.newPool);
                if (ret.state == "loaded") {
                    await this.loadAll();
                    Toaster.add("pool created!", `successfully created pool '${this.newPool}'`, "success");
                    this.newPool = "";
                } else if (ret.state == "error") {
                    Toaster.add("failed to create pool", `failed to create pool: ${ret.problem.detail}`, "danger");
                }

                return ret;
            }
        },

        computed: {
            hasCreatePoolPermission: function(): boolean {
                return AccountUtil.hasPermission("App.Pool.Create");
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