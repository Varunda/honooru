
<template>
    <div>

        <div v-if="posts.state == 'idle'"></div>

        <div v-else-if="posts.state == 'loading'">
            loading...
        </div>

        <div v-else-if="posts.state == 'loaded'" style="grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); display: grid; gap: 0.5rem;">
            <div v-for="post in posts.data.results">
                <a :href="'/post/' + post.id" style="max-height: 180px; max-width: 180px;">
                    <img :src="'/media/180x180/' + post.md5 + '.png'" />
                </a>
            </div>

            <div v-if="posts.data.results.length == 0">
                no posts found!
            </div>
        </div>

        <div v-else-if="posts.state == 'error'">
            <api-error :error="posts.details"></api-error>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ApiError from "components/ApiError";

    import { Post, SearchResults, PostApi } from "api/PostApi";

    export const PostList = Vue.extend({
        props: {
            q: { type: String, required: true },
            limit: { type: Number, required: false, default: 100 }
        },

        data: function() {
            return {
                posts: Loadable.idle() as Loading<SearchResults>
            }
        },

        mounted: function(): void {
            this.search();
        },

        methods: {
            search: async function(): Promise<void> {
                console.log(`searching [query=${this.q}]`);
                this.posts = Loadable.loading();
                this.posts = await PostApi.search(this.q, this.limit);
            },
        },

        watch: {
            q: async function(): Promise<void> {
                this.search();
            }
        },

        components: {
            ApiError
        }

    });
    export default PostList;
</script>