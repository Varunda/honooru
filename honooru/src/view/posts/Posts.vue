<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Posts
            </li>
        </app-menu>

        <hr class="border" />

        <div>

            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search @do-search="performSearch"></post-search>
                </div>

                <div>
                    <div v-if="posts.state == 'idle'"></div>

                    <div v-else-if="posts.state == 'loading'">
                        Loading...
                    </div>

                    <div v-else-if="posts.state == 'loaded'">
                        <post-list :posts="posts.data.results"></post-list>

                    </div>

                    <div v-else-if="posts.state == 'error'">
                        <api-error :error="posts.problem"></api-error>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PostSearch from "components/app/PostSearch.vue";

    import PostList from "./components/PostList.vue";

    import { Post, SearchResults, PostApi } from "api/PostApi";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,

                posts: Loadable.idle() as Loading<SearchResults>,
            }
        },

        beforeMount: function(): void {
            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "sort:id_desc";
            }
        },

        mounted: function(): void {
            this.search();
        },

        methods: {
            search: async function(): Promise<void> {
                console.log(`searching [query=${this.query}]`);
                this.posts = Loadable.loading();
                this.posts = await PostApi.search(this.query);
            },

            performSearch: function(query: string): void {
                this.query = query;
                this.search();

                const url = new URL(location.href);
                url.searchParams.set("q", this.query);
                history.pushState({ path: url.href }, "", `/posts?${url.searchParams.toString()}`);
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            PostList,
            PostSearch
        }
    });
    export default Posts;

</script>