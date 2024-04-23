<template>
    <div>
        <div v-if="pools.state == 'idle'"></div>

        <div v-else-if="pools.state == 'loading'">
            loading...
        </div>

        <div v-else-if="pools.state == 'loaded'">
            <div class="pool-list">
                <div class="pool-list-item bg-secondary py-1 px-2 rounded rounded-bottom-0">
                    post pools
                    <span>
                        ({{pools.data.length}})
                    </span>
                </div>

                <div v-for="pool in pools.data" class="pool-list-item">

                    <span v-if="poolsWithPost.has(pool.id) == false" class="pool-list-item-add" title="click to add this post to this pool" @click="addToPool(pool.id)">
                        <span class="bi-plus"></span>
                        {{pool.name}}
                    </span>

                    <span v-else class="pool-list-item-remove" title="click to remove post from pool" @click="removeFromPool(pool.id)">
                        <span class="bi-dash"></span>
                        {{pool.name}}
                    </span>

                    <span class="pool-list-item-open">
                        <a :href="'/pool/' + pool.id" title="open pool in new tab" target="_blank" rel="nofollow">
                            <span class="bi-box-arrow-up-right"></span>
                        </a>
                    </span>

                </div>
            </div>
        </div>

        <div v-else-if="pools.state == 'error'">
            <api-error :error="pools.problem"></api-error>
        </div>

        <div v-else>
            unchecked state of pools.data: {{pools.state}}
        </div>

        <div class="input-group mt-3">
            <input v-model="create" class="form-control" @keyup.enter="createPool" />

            <button class="btn btn-primary input-group-button" @click="createPool">
                create
            </button>
        </div>
    </div>

</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import { PostPool, PostPoolApi } from "api/PostPoolApi";
    import { PostPoolEntryApi } from "api/PostPoolEntryApi";

    export const PostPoolView = Vue.extend({
        props: {
            PostId: { type: Number, required: true }
        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<PostPool[]>,

                postPools: Loadable.idle() as Loading<PostPool[]>,

                poolsWithPost: new Set() as Set<number>,

                create: "" as string
            }
        },

        mounted: function(): void {
            this.loadPostsPools();
            this.loadPools();
        },

        methods: {
            loadPools: async function(): Promise<void> {
                console.log(`PostPoolView> loading pools`);
                this.pools = Loadable.loading();
                this.pools = await PostPoolApi.getAll();
            },

            loadPostsPools: async function(): Promise<void> {
                this.postPools = Loadable.loading();
                const l: Loading<PostPool[]> = await PostPoolEntryApi.getPoolsOfPost(this.PostId);

                this.poolsWithPost.clear();
                if (l.state == "loaded") {
                    for (const pool of l.data) {
                        this.poolsWithPost.add(pool.id);
                    }
                }

                this.$forceUpdate();

                this.postPools = l;
            },

            createPool: async function(): Promise<void> {
                if (this.create.length == 0) {
                    console.warn(`not creating pool, already exists!`);
                    return;
                }

                const l: Loading<PostPool> = await PostPoolApi.create(this.create);
                if (l.state == "loaded") {
                    Toaster.add(`pool created`, `successsfully create a pool named ${this.create}`, "success");
                    this.loadPools();
                } else if (l.state == "error") {
                    console.log(`PostPoolView> error creating pool: ${l.problem.detail}`);
                    Loadable.toastError(l, "failed to create post pool");
                } else {
                    console.error(`unchecked response status (create pool): ${l.state}`);
                }
            },

            addToPool: async function(poolID: number): Promise<void> {
                console.log(`PostPoolView> adding post ${this.PostId} to pool ${poolID}`);

                const l: Loading<void> = await PostPoolEntryApi.addToPool(poolID, this.PostId);
                if (l.state == "loaded") {
                    Toaster.add("post added", "post added to pool", "success");
                    this.loadPostsPools();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to add post to pool");
                } else {
                    console.error(`unchecked response state (add to pool): ${l.state}`);
                }
            },

            removeFromPool: async function(poolID: number): Promise<void> {
                console.log(`PostPoolView> removing post ${this.PostId} from pool ${poolID}`);

                const l: Loading<void> = await PostPoolEntryApi.removeFromPool(poolID, this.PostId);
                if (l.state == "loaded") {
                    Toaster.add("post removed", "post removed from pool", "success");
                    this.loadPostsPools();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to remove post from pool");
                } else {
                    console.error(`unchecked response state (remove from pool): ${l.state}`);
                }
            }
        },

        components: {

        }

    });
    export default PostPoolView;

</script>