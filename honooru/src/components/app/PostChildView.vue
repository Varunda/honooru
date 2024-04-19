<template>
    <div>
        <div class="mb-3 border-bottom pb-3">
            <h4 class="mb-0">
                <button class="btn btn-sm btn-primary" @click="addParent">
                    add
                </button>

                parents
                <span v-if="parents.state == 'loaded'" class="text-muted">
                    ({{parentPosts.length}})
                </span>
            </h4>
            <div v-if="parents.state == 'idle'"></div>

            <div v-else-if="parents.state == 'loading'">
                loading...
            </div>

            <div v-else-if="parents.state == 'loaded'">
                <post-thumbnail v-for="post in parentPosts" :key="post.id" :post="post" class="me-1"></post-thumbnail>
            </div>

            <div v-else-if="parent.state == 'error'">
                <api-error :error="parent.problem"></api-error>
            </div>

            <div v-else>
                unchecked value of parent.state: {{parent.state}}
            </div>
        </div>

        <div>
            <h4 class="mb-0">
                <button class="btn btn-sm btn-primary" @click="addChild">
                    add
                </button>

                children
                <span v-if="children.state == 'loaded'" class="text-muted">
                    ({{childPosts.length}})
                </span>
            </h4>
            <div v-if="children.state == 'idle'"></div>

            <div v-else-if="children.state == 'loading'">
                loading...
            </div>

            <div v-else-if="children.state == 'loaded'">
                <post-thumbnail v-for="post in childPosts" :key="post.id" :post="post"></post-thumbnail>
            </div>

            <div v-else-if="children.state == 'error'">
                <api-error :error="children.problem"></api-error>
            </div>

            <div v-else>
                unchecked value of children.state: {{children.state}}
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import { ExtendedPostChild, PostChild, PostChildApi } from "api/PostChildApi";
    import { Post } from "api/PostApi";

    import PostThumbnail from "components/app/PostThumbnail.vue";

    export const PostChildView = Vue.extend({
        props: {
            PostId: { type: Number, required: true }
        },

        data: function() {
            return {
                parents: Loadable.idle() as Loading<ExtendedPostChild[]>,
                children: Loadable.idle() as Loading<ExtendedPostChild[]>
            }
        },

        mounted: function(): void {
            this.bindAll();
        },

        methods: {
            bindAll: function(): void {
                this.bindParents();
                this.bindChildren();
            },

            bindParents: async function(): Promise<void> {
                // isn't this backwards? we're setting children based on the parent!
                // well yes, we want posts that are the child of this post, which means this post is the parent
                this.children = Loadable.loading();
                this.children = await PostChildApi.getByParentID(this.PostId);
            },

            bindChildren: async function(): Promise<void> {
                this.parents = Loadable.loading();
                this.parents = await PostChildApi.getByChildID(this.PostId);
            },

            addParent: async function(): Promise<void> {
                const input: string | null = prompt("enter ID of post to add as a parent (leave blank to cancel)");
                if (input == null) {
                    return;
                }

                const num: number = Number.parseInt(input);
                if (Number.isNaN(num) == true) {
                    Toaster.add("bad input", `input ${input} is not a valid number (parsed to ${num})`, "danger");
                    return;
                }

                const l: Loading<PostChild> = await PostChildApi.insert(num, this.PostId);
                if (l.state == "loaded") {
                    Toaster.add("parent added", `successfully added #${num} as a parent to this post (#${this.PostId})`, "success");
                    this.bindAll();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "error adding parent");
                } else {
                    console.error(`unchecked state of l: ${l.state}`);
                }
            },

            addChild: async function(): Promise<void> {
                const input: string | null = prompt("enter ID of post to add as a child (leave blank to cancel)");
                if (input == null) {
                    return;
                }

                const num: number = Number.parseInt(input);
                if (Number.isNaN(num) == true) {
                    Toaster.add("bad input", `input ${input} is not a valid number (parsed to ${num})`, "danger");
                    return;
                }

                const l: Loading<PostChild> = await PostChildApi.insert(this.PostId, num);
                if (l.state == "loaded") {
                    Toaster.add("child added", `successfully added #${num} as a child to this post (#${this.PostId})`, "success");
                    this.bindAll();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "error adding child");
                } else {
                    console.error(`unchecked state of l: ${l.state}`);
                }
            }
        },

        computed: {
            parentPosts: function(): Post[] {
                if (this.parents.state != "loaded") {
                    return [];
                }

                return this.parents.data.filter(iter => iter.parent != null).map(iter => iter.parent!);
            },

            childPosts: function(): Post[] {
                if (this.children.state != "loaded") {
                    return [];
                }

                return this.children.data.filter(iter => iter.child != null).map(iter => iter.child!);
            }

        },

        components: {
            PostThumbnail
        }
    });
    export default PostChildView;
</script>