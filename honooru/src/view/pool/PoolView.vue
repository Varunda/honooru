<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div v-if="pool.state == 'idle'"></div>

            <div v-else-if="pool.state == 'loading'">
                loading...
            </div>

            <div v-else-if="pool.state == 'loaded'">
                <div class="pb-3 mb-3 border-bottom">
                    <h1 class="d-inline">
                        pool
                        #{{pool.data.id}}:
                        {{pool.data.name}}
                    </h1>
                    <h4>
                        created by
                        <user-account :account-id="pool.data.createdByID"></user-account>
                        at
                        {{pool.data.timestamp | moment}}
                    </h4>
                </div>

                <post-list :q="'pool:' + poolID" :limit="500"></post-list>
            </div>

            <div v-else-if="pool.state == 'error'">
                <api-error :error="pool.problem"></api-error>
            </div>

            <div v-else>
                unchecked state of pool: {{pool.state}}
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
    import PostList from "components/app/PostList.vue";
    import UserAccount from "components/app/UserAccount.vue";

    import "filters/MomentFilter";

    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    export const PoolView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                poolID: 0 as number,
                pool: Loadable.idle() as Loading<PostPool>
            }
        },

        created: function(): void {
            this.getPoolIdFromUrl();
        },

        mounted: function(): void {
            if (this.poolID != 0) {
                this.loadPool();
            }
        },

        methods: {
            getPoolIdFromUrl: function(): void {
                const parts: string[] = location.pathname.split("/");
                if (parts.length < 3) {
                    console.error(`missing post ID`);
                    return;
                }

                this.poolID = Number.parseInt(parts[2]);

                if (Number.isNaN(this.poolID)) {
                    console.error(`invalid pool ID, turns into NaN! ${parts[2]}`);
                }

                document.title = `Honooru / Pool #${this.poolID}`;
            },

            loadPool: async function(): Promise<void> {
                this.pool = Loadable.loading();
                this.pool = await PostPoolApi.getByID(this.poolID);
            }

        },

        components: {
            InfoHover, ApiError, AlertCollapse,
            AppMenu,
            PostList, 
            UserAccount
        }
    });
    export default PoolView;
</script>