<template>
    <nav>
        <ul class="pagination justify-content-center">
            <li class="page-item">
                <a class="page-link bi-chevron-double-left" :href="links.first" :class="{ 'disabled': currentPage == 0 }"></a>
            </li>
            <li class="page-item">
                <a class="page-link bi-chevron-left" :href="links.previous" :class="{ 'disabled': currentPage == 0 }"></a>
            </li>

            <li v-for="p in shownPageCount" class="page-item">
                <a class="page-link" :class="{ 'active': p == 1 }" :href="'/posts?q=' + query + '&offset=' + ((p + currentPage - 1) * limit)">
                    {{p + currentPage}}
                </a>
            </li>

            <li v-if="pagesLeft > 10" class="page-item">
                <a class="page-link" href="javascript:void" data-bs-toggle="dropdown">
                    ...
                </a>

                <div class="dropdown-menu">
                    <form class="my-n2">
                        <div class="input-group">
                            <input v-model.number="pageNumber" type="number" class="form-control" placeholder="page number" @keyup.enter="goToPage" />
                            <button class="btn btn-primary" @click.stop="goToPage">go</button>
                        </div>
                    </form>
                </div>
            </li>

            <li v-if="pagesLeft > 10" class="page-item">
                <a class="page-link" :href="links.last">
                    {{pageCount}}
                </a>
            </li>

            <li class="page-item">
                <a class="page-link bi-chevron-right" :href="links.next" :class="{ 'disabled': !nextPageEnabled }"></a>
            </li>
            <li class="page-item">
                <a class="page-link bi-chevron-double-right" :href="links.last" :class="{ 'disabled': !nextPageEnabled }"></a>
            </li>
        </ul>
    </nav>
</template>

<script lang="ts">
    import Vue from "vue";

    export const PostsPage = Vue.extend({
        props: {
            query: { type: String, required: true },
            offset: { type: Number, required: true },
            limit: { type: Number, required: true },
            PostCount: { type: Number, required: true }
        },

        data: function() {
            return {
                pageNumber: 0 as number,
            }
        },

        mounted: function(): void {
            this.pageNumber = this.currentPage;
        },

        methods: {
            goToPage: function(): void {
                const url: string = `/posts?q=${this.query}&offset=${this.pageNumber * this.limit}`;
                console.log(`PostsPage> going to page ${this.pageNumber} [url=${url}]`);
                location.href = url;
            }
        },

        computed: {
            links: function() {
                // can't use this.query in the return{} for some reason, but it works out here /shrug
                const query: string = this.query;
                const currentPage: number = this.currentPage;
                const limit: number = this.limit;
                const pageCount: number = this.pageCount;

                return {
                    first: `/posts?q=${query}`,
                    previous: `/posts?q=${query}&offset=${Math.max(0, currentPage - 1) * limit}`,
                    next: `/posts?q=${query}&offset=${(currentPage + 1) * limit}`,
                    last: `/posts?q=${query}&offset=${((pageCount - 1) * limit)}`
                }
            },

            // what the current page is
            currentPage: function(): number {
                return Math.ceil(this.offset / this.limit);
            },

            // how many pages in total are in this query
            pageCount: function(): number {
                return Math.ceil(this.PostCount / this.limit);
            },

            shownPageCount: function(): number {
                return Math.max(1, Math.min(10, this.pageCount - this.currentPage));
            },

            // how many pages are left in this query, based on the offset into the query
            pagesLeft: function(): number {
                return this.pageCount - this.currentPage;
            },

            // if the next page button is enabled
            nextPageEnabled: function(): boolean {
                console.log(`currentPage=${this.currentPage}, pageCount=${this.pageCount}, Math.ceil=${Math.ceil(this.offset / this.limit)}`);
                return this.currentPage < (this.pageCount - 1);
            }

        }
    });
    export default PostsPage;
</script>