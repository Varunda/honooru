<template>
    <div>
        <app-menu></app-menu>

        <div>

            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search v-model="query" @keyup.enter="performSearch"></post-search>
                </div>

                <div>
                    <post-list :q="search"></post-list>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,
                search: "" as string,
            }
        },

        beforeMount: function(): void {
            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "sort:id_desc";
            }
            this.search = this.query;
        },

        methods: {
            performSearch: function(query: string): void {
                query = this.query;

                const url = new URL(location.href);
                url.searchParams.set("q", query.trim());
                history.pushState({ path: url.href }, "", `/posts?${url.searchParams.toString()}`);

                this.search = query;
                this.query = this.search;
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            PostList,
            PostSearch
        }
    });
    export default Posts;

</script>